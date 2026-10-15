using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Domain.Entities.Purchasing;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services.Purchasing;

public sealed class ReceptionService : IReceptionService
{
    public const string PurchaseEntryType = "ENTRADA_COMPRA";

    private const string ConcurrencyMessage =
        "El inventario cambió mientras se guardaba la recepción. Intente nuevamente.";

    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;
    private readonly PurchaseOrderService _orders;

    public ReceptionService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees,
        PurchaseOrderService orders)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
        _orders = orders;
    }

    public async Task<PagedResponse<ReceptionListItemResponse>> GetPagedAsync(
        ReceptionQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_RECEPCION_COMPRA> receptions = _context.RecepcionesCompra.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            string status = query.Status.Trim().ToUpperInvariant();
            receptions = receptions.Where(item => item.EstadoRecepcionCompra.ERC_Codigo == status);
        }

        if (query.PurchaseOrderId.HasValue)
        {
            receptions = receptions.Where(item => item.REC_OrdenCompraId == query.PurchaseOrderId.Value);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            receptions = receptions.Where(item => item.REC_FechaRecepcion >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            receptions = receptions.Where(item => item.REC_FechaRecepcion < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();

            receptions = receptions.Where(item =>
                item.REC_Numero.Contains(value) ||
                item.OrdenCompra.OCO_Numero.Contains(value) ||
                (item.REC_NumeroDocumentoProveedor != null && item.REC_NumeroDocumentoProveedor.Contains(value)) ||
                (item.OrdenCompra.Proveedor.Persona.PersonaJuridica != null &&
                 item.OrdenCompra.Proveedor.Persona.PersonaJuridica.PJU_RazonSocial.Contains(value)));
        }

        int totalItems = await receptions.CountAsync(cancellationToken);

        List<ReceptionListItemResponse> items = await receptions
            .OrderByDescending(item => item.REC_FechaRecepcion)
            .ThenByDescending(item => item.REC_RecepcionCompra)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ReceptionListItemResponse
            {
                Id = item.REC_RecepcionCompra,
                Number = item.REC_Numero,
                PurchaseOrderId = item.REC_OrdenCompraId,
                PurchaseOrderNumber = item.OrdenCompra.OCO_Numero,
                SupplierName = item.OrdenCompra.Proveedor.Persona.PersonaJuridica != null
                    ? item.OrdenCompra.Proveedor.Persona.PersonaJuridica.PJU_RazonSocial
                    : item.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.OrdenCompra.Proveedor.Persona.PersonaNatural.PNA_PrimerApellido,
                Warehouse = item.Almacen.ALM_Nombre,
                StatusCode = item.EstadoRecepcionCompra.ERC_Codigo,
                Status = item.EstadoRecepcionCompra.ERC_Nombre,
                ReceivedAt = item.REC_FechaRecepcion,
                SupplierDocument = item.REC_NumeroDocumentoProveedor == null
                    ? null
                    : (item.REC_SerieDocumentoProveedor != null ? item.REC_SerieDocumentoProveedor + "-" : "") +
                      item.REC_NumeroDocumentoProveedor,
                AcceptedUnits = item.Detalles.Sum(line => line.RCD_CantidadAceptada),
                RejectedUnits = item.Detalles.Sum(line => line.RCD_CantidadRechazada),
                TotalCost = item.Detalles.Sum(line => line.RCD_CantidadAceptada * line.RCD_CostoUnitario)
            })
            .ToListAsync(cancellationToken);

        foreach (ReceptionListItemResponse item in items)
        {
            item.TotalCost = LineAmounts.Round(item.TotalCost);
        }

        return new PagedResponse<ReceptionListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ReceptionResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ReceptionResponse? response = await _context.RecepcionesCompra
            .AsNoTracking()
            .Where(item => item.REC_RecepcionCompra == id)
            .Select(item => new ReceptionResponse
            {
                Id = item.REC_RecepcionCompra,
                Number = item.REC_Numero,
                PurchaseOrderId = item.REC_OrdenCompraId,
                PurchaseOrderNumber = item.OrdenCompra.OCO_Numero,
                PurchaseOrderStatus = item.OrdenCompra.EstadoOrdenCompra.EOC_Nombre,
                SupplierId = item.OrdenCompra.OCO_ProveedorId,
                SupplierName = item.OrdenCompra.Proveedor.Persona.PersonaJuridica != null
                    ? item.OrdenCompra.Proveedor.Persona.PersonaJuridica.PJU_RazonSocial
                    : item.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.OrdenCompra.Proveedor.Persona.PersonaNatural.PNA_PrimerApellido,
                WarehouseId = item.REC_AlmacenId,
                Warehouse = item.Almacen.ALM_Nombre,
                StatusCode = item.EstadoRecepcionCompra.ERC_Codigo,
                Status = item.EstadoRecepcionCompra.ERC_Nombre,
                ReceivedAt = item.REC_FechaRecepcion,
                SupplierDocumentSeries = item.REC_SerieDocumentoProveedor,
                SupplierDocumentNumber = item.REC_NumeroDocumentoProveedor,
                ReceivedBy = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                             item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                             " (" + item.Empleado.EMP_Codigo + ")",
                MovementNumber = item.MovimientosInventario
                    .OrderBy(movement => movement.MOV_MovimientoInventario)
                    .Select(movement => movement.MOV_Numero)
                    .FirstOrDefault(),
                Notes = item.REC_Observaciones,
                Lines = item.Detalles
                    .OrderBy(line => line.RCD_RecepcionCompraDetalle)
                    .Select(line => new ReceptionLineResponse
                    {
                        Id = line.RCD_RecepcionCompraDetalle,
                        PurchaseOrderLineId = line.RCD_OrdenCompraDetalleId,
                        BookId = line.RCD_LibroId,
                        BookCode = line.Libro.LIB_Codigo,
                        Title = line.Libro.LIB_Titulo,
                        Received = line.RCD_CantidadRecibida,
                        Accepted = line.RCD_CantidadAceptada,
                        Rejected = line.RCD_CantidadRechazada,
                        UnitCost = line.RCD_CostoUnitario,
                        RejectReason = line.RCD_MotivoRechazo,
                        LotNumber = line.Lote != null ? line.Lote.LOT_Numero : null,
                        Notes = line.RCD_Observaciones
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
        {
            response.TotalCost = LineAmounts.Round(response.Lines.Sum(line => line.Accepted * line.UnitCost));
            response.CanAnnul = response.StatusCode == ReceptionStatus.Processed;
        }

        return response;
    }

    public async Task<ReceptionCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        return new ReceptionCatalogsResponse
        {
            Warehouses = await _context.Almacenes
                .AsNoTracking()
                .Where(item => item.ALM_Activo)
                .OrderByDescending(item => item.ALM_EsPrincipal)
                .ThenBy(item => item.ALM_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.ALM_Almacen, Code = item.ALM_Codigo, Name = item.ALM_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<OperationResult<ReceptionResponse>> RegisterAsync(
        RegisterReceptionRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<ReceptionResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_ORDEN_COMPRA? order = await LoadOrderAsync(request.PurchaseOrderId!.Value, token);

                    if (order is null)
                    {
                        return OperationResult<ReceptionResponse>.Missing("La orden de compra no existe.");
                    }

                    List<LineToReceive> lines = [];
                    List<string> errors = [];

                    for (int index = 0; index < request.Lines.Count; index++)
                    {
                        ReceptionLineRequest line = request.Lines[index];
                        PB_ORDEN_COMPRA_DETALLE? detail = order.Detalles
                            .FirstOrDefault(item => item.OCD_OrdenCompraDetalle == line.PurchaseOrderLineId);

                        if (detail is null)
                        {
                            errors.Add($"Línea {index + 1}: no pertenece a la orden {order.OCO_Numero}.");
                            continue;
                        }

                        lines.Add(new LineToReceive(detail, line.Accepted, line.Rejected, line.RejectReason, line.UnitCost, line.Notes));
                    }

                    if (errors.Count > 0)
                    {
                        return OperationResult<ReceptionResponse>.Failure(errors.ToArray());
                    }

                    return await ReceiveAsync(order, request, lines, employee, token);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ReceptionResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<ReceptionResponse>> RegisterDirectPurchaseAsync(
        DirectPurchaseRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<ReceptionResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    OperationResult<PB_ORDEN_COMPRA> created = await _orders.CreateIssuedAsync(request.Order!, employee, token);

                    if (!created.Succeeded)
                    {
                        return created.ErrorType == OperationErrorType.Conflict
                            ? OperationResult<ReceptionResponse>.Conflict(created.Errors.ToArray())
                            : OperationResult<ReceptionResponse>.Failure(created.Errors.ToArray());
                    }

                    PB_ORDEN_COMPRA order = (await LoadOrderAsync(created.Data!.OCO_OrdenCompra, token))!;

                    List<LineToReceive> lines = order.Detalles
                        .Select(detail => new LineToReceive(detail, (int)detail.OCD_CantidadSolicitada, 0, null, null, null))
                        .ToList();

                    return await ReceiveAsync(order, request, lines, employee, token);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ReceptionResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<ReceptionResponse>> AnnulAsync(
        int id,
        AnnulReceptionRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<ReceptionResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_RECEPCION_COMPRA? reception = await _context.RecepcionesCompra
                        .Include(item => item.EstadoRecepcionCompra)
                        .Include(item => item.Detalles).ThenInclude(line => line.Lote)
                        .Include(item => item.Detalles).ThenInclude(line => line.Libro)
                        .Include(item => item.MovimientosInventario)
                        .FirstOrDefaultAsync(item => item.REC_RecepcionCompra == id, token);

                    if (reception is null)
                    {
                        return OperationResult<ReceptionResponse>.Missing("La recepción no existe.");
                    }

                    if (reception.EstadoRecepcionCompra.ERC_Codigo != ReceptionStatus.Processed)
                    {
                        return OperationResult<ReceptionResponse>.Conflict($"La recepción {reception.REC_Numero} ya está anulada.");
                    }

                    // Solo se revierte si nada de lo recibido ha salido del inventario.
                    string[] used = reception.Detalles
                        .Where(line => line.Lote is not null && line.Lote.LOT_CantidadActual < line.RCD_CantidadAceptada)
                        .Select(line => line.Libro.LIB_Titulo)
                        .ToArray();

                    if (used.Length > 0)
                    {
                        return OperationResult<ReceptionResponse>.Conflict(
                            "No se puede anular la recepción porque ya hubo salidas de inventario de: " +
                            string.Join(", ", used) + ".");
                    }

                    PB_ORDEN_COMPRA order = (await LoadOrderAsync(reception.REC_OrdenCompraId, token))!;
                    DateTime now = DateTime.UtcNow;

                    foreach (PB_RECEPCION_COMPRA_DETALLE line in reception.Detalles.Where(line => line.RCD_CantidadAceptada > 0))
                    {
                        PB_EXISTENCIA stock = await _context.Existencias.SingleAsync(
                            item => item.EXI_AlmacenId == reception.REC_AlmacenId && item.EXI_LibroId == line.RCD_LibroId, token);

                        if (stock.EXI_CantidadDisponible - stock.EXI_CantidadReservada < line.RCD_CantidadAceptada)
                        {
                            return OperationResult<ReceptionResponse>.Conflict(
                                $"No hay existencia libre suficiente de \"{line.Libro.LIB_Titulo}\" para revertir la recepción.");
                        }

                        stock.EXI_CantidadDisponible -= line.RCD_CantidadAceptada;
                        stock.EXI_FechaUltimoMovimiento = now;
                        stock.EXI_FechaModificacion = now;

                        line.Lote!.LOT_CantidadActual = 0;
                        line.Lote.LOT_Activo = false;
                        line.Lote.LOT_FechaModificacion = now;

                        PB_ORDEN_COMPRA_DETALLE detail = order.Detalles.Single(item => item.OCD_OrdenCompraDetalle == line.RCD_OrdenCompraDetalleId);
                        detail.OCD_CantidadRecibida -= line.RCD_CantidadAceptada;
                    }

                    string reason = request.Reason.Trim();

                    foreach (PB_MOVIMIENTO_INVENTARIO movement in reception.MovimientosInventario.Where(item => !item.MOV_Anulado))
                    {
                        movement.MOV_Anulado = true;
                        movement.MOV_FechaAnulacion = now;
                        movement.MOV_MotivoAnulacion = reason;
                    }

                    reception.REC_EstadoRecepcionCompraId = await ReceptionStatusIdAsync(ReceptionStatus.Annulled, token);
                    reception.REC_Observaciones = Append(reception.REC_Observaciones, $"[Anulada el {now:dd/MM/yyyy} por {employee.Code}] {reason}");
                    reception.REC_FechaModificacion = now;

                    await UpdateOrderStatusAsync(order, token);
                    await _context.SaveChangesAsync(token);

                    return OperationResult<ReceptionResponse>.Success((await GetByIdAsync(id, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ReceptionResponse>.Conflict(ConcurrencyMessage);
        }
    }

    /// <summary>
    /// Valida lo recibido contra la orden y registra recepción, lotes,
    /// existencias, movimiento de entrada y estado de la orden. Se ejecuta
    /// dentro de la transacción del llamador.
    /// </summary>
    private async Task<OperationResult<ReceptionResponse>> ReceiveAsync(
        PB_ORDEN_COMPRA order,
        ReceptionHeaderRequest header,
        IReadOnlyList<LineToReceive> lines,
        CurrentEmployee employee,
        CancellationToken cancellationToken)
    {
        List<string> errors = [];

        if (order.EstadoOrdenCompra.EOC_Codigo is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.Partial))
        {
            return OperationResult<ReceptionResponse>.Conflict(
                $"Solo se reciben órdenes emitidas o recibidas parcialmente; la orden {order.OCO_Numero} está " +
                $"{order.EstadoOrdenCompra.EOC_Nombre.ToLowerInvariant()}.");
        }

        foreach (LineToReceive line in lines)
        {
            decimal pending = line.Detail.OCD_CantidadSolicitada - line.Detail.OCD_CantidadRecibida;

            if (line.Accepted + line.Rejected > pending)
            {
                errors.Add(
                    $"\"{line.Detail.Libro.LIB_Titulo}\": se indicaron {line.Accepted + line.Rejected} unidades " +
                    $"pero solo hay {pending:0} pendientes de recibir.");
            }
        }

        if (lines.All(line => line.Accepted == 0))
        {
            errors.Add("Debe aceptar al menos una unidad para registrar la entrada a inventario.");
        }

        DateOnly today = BusinessTime.Today;
        DateOnly receivedOn = header.ReceivedOn ?? today;

        if (receivedOn < BusinessTime.ToLocalDate(order.OCO_FechaEmision))
        {
            errors.Add("La fecha de recepción no puede ser anterior a la emisión de la orden.");
        }

        var warehouse = await ResolveWarehouseAsync(header.WarehouseId, order.OCO_SucursalId, cancellationToken);

        if (warehouse is null)
        {
            errors.Add("El almacén seleccionado no existe o está inactivo.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<ReceptionResponse>.Failure(errors.ToArray());
        }

        string? series = string.IsNullOrWhiteSpace(header.SupplierDocumentSeries) ? null : header.SupplierDocumentSeries.Trim().ToUpperInvariant();
        string? number = string.IsNullOrWhiteSpace(header.SupplierDocumentNumber) ? null : header.SupplierDocumentNumber.Trim();

        if (number is not null)
        {
            string? duplicate = await _context.RecepcionesCompra
                .Where(item =>
                    item.REC_NumeroDocumentoProveedor == number &&
                    item.REC_SerieDocumentoProveedor == series &&
                    item.OrdenCompra.OCO_ProveedorId == order.OCO_ProveedorId &&
                    item.EstadoRecepcionCompra.ERC_Codigo != ReceptionStatus.Annulled)
                .Select(item => item.REC_Numero)
                .FirstOrDefaultAsync(cancellationToken);

            if (duplicate is not null)
            {
                return OperationResult<ReceptionResponse>.Conflict(
                    $"La factura {(series is null ? "" : series + "-")}{number} de este proveedor ya se registró en la recepción {duplicate}.");
            }
        }

        DateTime now = DateTime.UtcNow;
        DateTime receivedAt = receivedOn == today
            ? now
            : TimeZoneInfo.ConvertTimeToUtc(receivedOn.ToDateTime(new TimeOnly(12, 0)), BusinessTime.Zone);
        string receptionNumber = await _numbers.NextAsync(DocumentTypes.PurchaseReception, cancellationToken);

        PB_RECEPCION_COMPRA reception = new()
        {
            REC_Numero = receptionNumber,
            REC_OrdenCompraId = order.OCO_OrdenCompra,
            REC_AlmacenId = warehouse!.Id,
            REC_EstadoRecepcionCompraId = await ReceptionStatusIdAsync(ReceptionStatus.Processed, cancellationToken),
            REC_EmpleadoId = employee.Id,
            REC_FechaRecepcion = receivedAt,
            REC_SerieDocumentoProveedor = series,
            REC_NumeroDocumentoProveedor = number,
            REC_Observaciones = string.IsNullOrWhiteSpace(header.Notes) ? null : header.Notes.Trim(),
            REC_ActualizaInventario = true,
            REC_FechaProcesado = now,
            REC_FechaCreacion = now
        };

        PB_MOVIMIENTO_INVENTARIO movement = new()
        {
            MOV_Numero = await _numbers.NextAsync(DocumentTypes.InventoryMovement, cancellationToken),
            MOV_TipoMovimientoId = await _context.TiposMovimientoInventario
                .Where(type => type.TMI_Codigo == PurchaseEntryType)
                .Select(type => type.TMI_TipoMovimientoInventario)
                .SingleAsync(cancellationToken),
            MOV_AlmacenDestinoId = warehouse.Id,
            RecepcionCompra = reception,
            MOV_EmpleadoId = employee.Id,
            MOV_Fecha = receivedAt,
            MOV_DocumentoReferencia = number is null
                ? $"{order.OCO_Numero} / {receptionNumber}"
                : $"{order.OCO_Numero} / Factura {(series is null ? "" : series + "-")}{number}",
            MOV_Procesado = true,
            MOV_FechaProcesado = now,
            MOV_FechaCreacion = now
        };

        foreach (LineToReceive line in lines.Where(line => line.Accepted + line.Rejected > 0))
        {
            PB_ORDEN_COMPRA_DETALLE detail = line.Detail;
            decimal unitCost = line.UnitCost ?? detail.OCD_CostoUnitario;
            decimal netLocalCost = decimal.Round(
                unitCost * (1 - detail.OCD_PorcentajeDescuento / 100m) * order.OCO_TipoCambio,
                4,
                MidpointRounding.AwayFromZero);

            PB_RECEPCION_COMPRA_DETALLE receptionLine = new()
            {
                RCD_OrdenCompraDetalleId = detail.OCD_OrdenCompraDetalle,
                RCD_LibroId = detail.OCD_LibroId,
                RCD_CantidadRecibida = line.Accepted + line.Rejected,
                RCD_CantidadAceptada = line.Accepted,
                RCD_CantidadRechazada = line.Rejected,
                RCD_CostoUnitario = netLocalCost,
                RCD_MotivoRechazo = line.Rejected > 0 ? line.RejectReason!.Trim() : null,
                RCD_Observaciones = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            };

            reception.Detalles.Add(receptionLine);

            if (line.Accepted == 0)
            {
                continue;
            }

            PB_LOTE lot = new()
            {
                LOT_AlmacenId = warehouse.Id,
                LOT_LibroId = detail.OCD_LibroId,
                LOT_ProveedorId = order.OCO_ProveedorId,
                LOT_Numero = receptionNumber,
                LOT_FechaIngreso = receivedOn,
                LOT_CantidadInicial = line.Accepted,
                LOT_CantidadActual = line.Accepted,
                LOT_CostoUnitario = netLocalCost,
                LOT_Activo = true,
                LOT_FechaCreacion = now
            };

            receptionLine.Lote = lot;

            movement.Detalles.Add(new PB_MOVIMIENTO_INVENTARIO_DETALLE
            {
                MDE_LibroId = detail.OCD_LibroId,
                Lote = lot,
                MDE_Cantidad = line.Accepted,
                MDE_CostoUnitario = netLocalCost,
                MDE_CostoTotal = LineAmounts.Round(line.Accepted * netLocalCost)
            });

            PB_EXISTENCIA? stock = await _context.Existencias.FirstOrDefaultAsync(
                item => item.EXI_AlmacenId == warehouse.Id && item.EXI_LibroId == detail.OCD_LibroId,
                cancellationToken);

            if (stock is null)
            {
                stock = new PB_EXISTENCIA
                {
                    EXI_AlmacenId = warehouse.Id,
                    EXI_LibroId = detail.OCD_LibroId,
                    EXI_FechaCreacion = now
                };

                _context.Existencias.Add(stock);
            }

            stock.EXI_CantidadDisponible += line.Accepted;
            stock.EXI_FechaUltimoMovimiento = now;
            stock.EXI_FechaModificacion = now;

            detail.OCD_CantidadRecibida += line.Accepted;

            // El costo de referencia del libro pasa a ser el último costo de compra.
            detail.Libro.LIB_CostoReferencia = LineAmounts.Round(netLocalCost);
            detail.Libro.LIB_FechaModificacion = now;
        }

        _context.RecepcionesCompra.Add(reception);
        _context.MovimientosInventario.Add(movement);

        await UpdateOrderStatusAsync(order, cancellationToken);
        order.OCO_FechaModificacion = now;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<ReceptionResponse>.Success((await GetByIdAsync(reception.REC_RecepcionCompra, cancellationToken))!);
    }

    private Task<PB_ORDEN_COMPRA?> LoadOrderAsync(int id, CancellationToken cancellationToken) =>
        _context.OrdenesCompra
            .Include(order => order.EstadoOrdenCompra)
            .Include(order => order.Detalles).ThenInclude(line => line.Libro)
            .FirstOrDefaultAsync(order => order.OCO_OrdenCompra == id && order.OCO_Activo, cancellationToken);

    /// <summary>Almacén indicado o, si no, el principal de la sucursal de la orden.</summary>
    private async Task<WarehouseData?> ResolveWarehouseAsync(int? warehouseId, int branchId, CancellationToken cancellationToken)
    {
        IQueryable<PB_ALMACEN> warehouses = _context.Almacenes.AsNoTracking().Where(item => item.ALM_Activo);

        if (warehouseId.HasValue)
        {
            return await warehouses
                .Where(item => item.ALM_Almacen == warehouseId.Value)
                .Select(item => new WarehouseData(item.ALM_Almacen))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await warehouses
            .Where(item => item.ALM_EsPrincipal)
            .OrderByDescending(item => item.ALM_SucursalId == branchId)
            .Select(item => new WarehouseData(item.ALM_Almacen))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Emitida (nada recibido), parcial o recibida según las cantidades.</summary>
    private async Task UpdateOrderStatusAsync(PB_ORDEN_COMPRA order, CancellationToken cancellationToken)
    {
        string status = order.Detalles.All(line => line.OCD_CantidadRecibida >= line.OCD_CantidadSolicitada)
            ? PurchaseOrderStatus.Received
            : order.Detalles.Any(line => line.OCD_CantidadRecibida > 0)
                ? PurchaseOrderStatus.Partial
                : PurchaseOrderStatus.Issued;

        order.OCO_EstadoOrdenCompraId = await _context.EstadosOrdenCompra
            .Where(item => item.EOC_Codigo == status)
            .Select(item => item.EOC_EstadoOrdenCompra)
            .SingleAsync(cancellationToken);
    }

    private Task<int> ReceptionStatusIdAsync(string code, CancellationToken cancellationToken) =>
        _context.EstadosRecepcionCompra
            .Where(item => item.ERC_Codigo == code)
            .Select(item => item.ERC_EstadoRecepcionCompra)
            .SingleAsync(cancellationToken);

    private static string Append(string? current, string note)
    {
        string text = string.IsNullOrWhiteSpace(current) ? note : $"{current}\n{note}";
        return text.Length > 1000 ? text[^1000..] : text;
    }

    private sealed record WarehouseData(int Id);

    private sealed record LineToReceive(
        PB_ORDEN_COMPRA_DETALLE Detail,
        int Accepted,
        int Rejected,
        string? RejectReason,
        decimal? UnitCost,
        string? Notes);
}
