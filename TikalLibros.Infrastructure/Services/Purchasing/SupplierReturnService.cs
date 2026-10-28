using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Domain.Entities.Purchasing;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Audit;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services.Purchasing;

/// <summary>
/// Devoluciones al proveedor. Se devuelve mercadería del lote que creó la
/// recepción, siempre que siga en el almacén y libre de reservas. El valor se
/// calcula al costo de compra (lo que el proveedor debe acreditar). Anular la
/// devolución regresa las unidades al mismo lote.
/// </summary>
public sealed class SupplierReturnService : ISupplierReturnService
{
    public const string SupplierReturnType = "DEVOLUCION_PROVEEDOR";

    private const string Entity = "DevolucionProveedor";

    private const string ConcurrencyMessage =
        "El inventario cambió mientras se guardaba la devolución. Intente nuevamente.";

    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;
    private readonly AuditTrail _audit;

    public SupplierReturnService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees,
        AuditTrail audit)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
        _audit = audit;
    }

    private IQueryable<PB_MOVIMIENTO_INVENTARIO> Returns =>
        _context.MovimientosInventario.AsNoTracking().Where(item => item.TipoMovimiento.TMI_Codigo == SupplierReturnType);

    public async Task<PagedResponse<SupplierReturnListItemResponse>> GetPagedAsync(
        SupplierReturnQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_MOVIMIENTO_INVENTARIO> returns = Returns.Where(item => item.RecepcionCompra != null);

        if (!query.IncludeAnnulled)
        {
            returns = returns.Where(item => !item.MOV_Anulado);
        }

        if (query.SupplierId.HasValue)
        {
            returns = returns.Where(item => item.RecepcionCompra!.OrdenCompra.OCO_ProveedorId == query.SupplierId);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            returns = returns.Where(item => item.MOV_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            returns = returns.Where(item => item.MOV_Fecha < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            returns = returns.Where(item =>
                item.MOV_Numero.Contains(value) ||
                item.RecepcionCompra!.REC_Numero.Contains(value) ||
                item.RecepcionCompra!.OrdenCompra.OCO_Numero.Contains(value) ||
                (item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaJuridica != null &&
                 item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaJuridica!.PJU_RazonSocial.Contains(value)));
        }

        int totalItems = await returns.CountAsync(cancellationToken);

        List<SupplierReturnListItemResponse> items = await returns
            .OrderByDescending(item => item.MOV_Fecha)
            .ThenByDescending(item => item.MOV_MovimientoInventario)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new SupplierReturnListItemResponse
            {
                Id = item.MOV_MovimientoInventario,
                Number = item.MOV_Numero,
                Date = item.MOV_Fecha,
                ReceptionId = item.MOV_RecepcionCompraId!.Value,
                ReceptionNumber = item.RecepcionCompra!.REC_Numero,
                Supplier = item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaJuridica != null
                    ? item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaJuridica!.PJU_RazonSocial
                    : item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerApellido,
                Warehouse = item.AlmacenOrigen!.ALM_Nombre,
                Units = item.Detalles.Sum(line => line.MDE_Cantidad),
                Total = item.Detalles.Sum(line => line.MDE_CostoTotal),
                Employee = item.Empleado != null
                    ? item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Empleado.Persona.PersonaNatural!.PNA_PrimerApellido
                    : null,
                Annulled = item.MOV_Anulado
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<SupplierReturnListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public Task<SupplierReturnResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Returns
            .Where(item => item.MOV_MovimientoInventario == id && item.RecepcionCompra != null)
            .Select(item => new SupplierReturnResponse
            {
                Id = item.MOV_MovimientoInventario,
                Number = item.MOV_Numero,
                Date = item.MOV_Fecha,
                ReceptionId = item.MOV_RecepcionCompraId!.Value,
                ReceptionNumber = item.RecepcionCompra!.REC_Numero,
                PurchaseOrderNumber = item.RecepcionCompra!.OrdenCompra.OCO_Numero,
                Supplier = item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaJuridica != null
                    ? item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaJuridica!.PJU_RazonSocial
                    : item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.RecepcionCompra!.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerApellido,
                SupplierDocument = item.RecepcionCompra!.REC_NumeroDocumentoProveedor,
                Warehouse = item.AlmacenOrigen!.ALM_Nombre,
                Employee = item.Empleado != null
                    ? item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.Empleado.Persona.PersonaNatural!.PNA_PrimerApellido + " (" + item.Empleado.EMP_Codigo + ")"
                    : null,
                Reason = item.MOV_Observaciones ?? string.Empty,
                Total = item.Detalles.Sum(line => line.MDE_CostoTotal),
                Annulled = item.MOV_Anulado,
                AnnulledAt = item.MOV_FechaAnulacion,
                AnnulmentReason = item.MOV_MotivoAnulacion,
                Lines = item.Detalles
                    .OrderBy(line => line.MDE_MovimientoInventarioDetalle)
                    .Select(line => new SupplierReturnLineResponse
                    {
                        BookId = line.MDE_LibroId,
                        BookCode = line.Libro.LIB_Codigo,
                        Title = line.Libro.LIB_Titulo,
                        LotNumber = line.Lote != null ? line.Lote.LOT_Numero : null,
                        Quantity = line.MDE_Cantidad,
                        UnitCost = line.MDE_CostoUnitario,
                        Total = line.MDE_CostoTotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ReturnableReceptionResponse?> GetReturnableAsync(int receptionId, CancellationToken cancellationToken = default)
    {
        var reception = await _context.RecepcionesCompra
            .AsNoTracking()
            .Where(item => item.REC_RecepcionCompra == receptionId && item.EstadoRecepcionCompra.ERC_Codigo == ReceptionStatus.Processed)
            .Select(item => new
            {
                item.REC_RecepcionCompra,
                item.REC_Numero,
                item.REC_FechaRecepcion,
                item.REC_AlmacenId,
                Supplier = item.OrdenCompra.Proveedor.Persona.PersonaJuridica != null
                    ? item.OrdenCompra.Proveedor.Persona.PersonaJuridica!.PJU_RazonSocial
                    : item.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerApellido,
                Warehouse = item.Almacen.ALM_Nombre
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (reception is null)
        {
            return null;
        }

        // Las líneas van en una consulta aparte (SQLite no admite la subconsulta correlacionada).
        var lines = await _context.RecepcionesCompraDetalles
            .AsNoTracking()
            .Where(line => line.RCD_RecepcionCompraId == receptionId && line.RCD_CantidadAceptada > 0)
            .OrderBy(line => line.RCD_RecepcionCompraDetalle)
            .Select(line => new
            {
                line.RCD_RecepcionCompraDetalle,
                line.RCD_LibroId,
                line.Libro.LIB_Codigo,
                line.Libro.LIB_Titulo,
                LotNumber = line.Lote != null ? line.Lote.LOT_Numero : null,
                LotWarehouse = line.Lote != null ? line.Lote.LOT_AlmacenId : 0,
                LotRemaining = line.Lote != null ? line.Lote.LOT_CantidadActual : 0,
                line.RCD_CantidadAceptada,
                line.RCD_CostoUnitario
            })
            .ToListAsync(cancellationToken);

        int[] books = lines.Select(line => line.RCD_LibroId).Distinct().ToArray();

        Dictionary<int, decimal> free = await _context.Existencias
            .AsNoTracking()
            .Where(item => item.EXI_AlmacenId == reception.REC_AlmacenId && books.Contains(item.EXI_LibroId))
            .ToDictionaryAsync(item => item.EXI_LibroId, item => item.EXI_CantidadDisponible - item.EXI_CantidadReservada, cancellationToken);

        return new ReturnableReceptionResponse
        {
            ReceptionId = reception.REC_RecepcionCompra,
            ReceptionNumber = reception.REC_Numero,
            ReceivedAt = reception.REC_FechaRecepcion,
            Supplier = reception.Supplier,
            Warehouse = reception.Warehouse,
            Lines = lines.Select(line => new ReturnableLineResponse
            {
                ReceptionLineId = line.RCD_RecepcionCompraDetalle,
                BookId = line.RCD_LibroId,
                BookCode = line.LIB_Codigo,
                Title = line.LIB_Titulo,
                LotNumber = line.LotNumber,
                Accepted = line.RCD_CantidadAceptada,
                Returnable = line.LotWarehouse != reception.REC_AlmacenId
                    ? 0
                    : Math.Max(0, Math.Min(line.LotRemaining, free.GetValueOrDefault(line.RCD_LibroId))),
                UnitCost = line.RCD_CostoUnitario
            }).ToList()
        };
    }

    public async Task<OperationResult<SupplierReturnResponse>> CreateAsync(
        SaveSupplierReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<SupplierReturnResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(async token =>
            {
                PB_RECEPCION_COMPRA? reception = await _context.RecepcionesCompra
                    .Include(item => item.EstadoRecepcionCompra)
                    .Include(item => item.Detalles).ThenInclude(line => line.Lote)
                    .Include(item => item.Detalles).ThenInclude(line => line.Libro)
                    .Include(item => item.OrdenCompra)
                    .FirstOrDefaultAsync(item => item.REC_RecepcionCompra == request.ReceptionId, token);

                if (reception is null)
                {
                    return OperationResult<SupplierReturnResponse>.Missing("La recepción no existe.");
                }

                if (reception.EstadoRecepcionCompra.ERC_Codigo != ReceptionStatus.Processed)
                {
                    return OperationResult<SupplierReturnResponse>.Conflict($"La recepción {reception.REC_Numero} está anulada; no admite devoluciones.");
                }

                List<string> errors = [];
                List<string> conflicts = [];
                DateTime now = DateTime.UtcNow;

                PB_MOVIMIENTO_INVENTARIO movement = new()
                {
                    MOV_Numero = await _numbers.NextAsync(DocumentTypes.InventoryMovement, token),
                    MOV_TipoMovimientoId = await _context.TiposMovimientoInventario
                        .Where(type => type.TMI_Codigo == SupplierReturnType)
                        .Select(type => type.TMI_TipoMovimientoInventario)
                        .SingleAsync(token),
                    MOV_AlmacenOrigenId = reception.REC_AlmacenId,
                    MOV_RecepcionCompraId = reception.REC_RecepcionCompra,
                    MOV_EmpleadoId = employee.Id,
                    MOV_Fecha = now,
                    MOV_DocumentoReferencia = $"{reception.REC_Numero} / {reception.OrdenCompra.OCO_Numero}",
                    MOV_Observaciones = request.Reason.Trim(),
                    MOV_Procesado = true,
                    MOV_FechaProcesado = now,
                    MOV_FechaCreacion = now
                };

                for (int index = 0; index < request.Lines.Count; index++)
                {
                    SupplierReturnLineRequest line = request.Lines[index];
                    PB_RECEPCION_COMPRA_DETALLE? detail = reception.Detalles.FirstOrDefault(item => item.RCD_RecepcionCompraDetalle == line.ReceptionLineId);

                    if (detail is null)
                    {
                        errors.Add($"Línea {index + 1}: no pertenece a la recepción {reception.REC_Numero}.");
                        continue;
                    }

                    decimal quantity = line.Quantity!.Value;
                    PB_LOTE? lot = detail.Lote;
                    PB_EXISTENCIA? stock = await _context.Existencias.FirstOrDefaultAsync(
                        item => item.EXI_AlmacenId == reception.REC_AlmacenId && item.EXI_LibroId == detail.RCD_LibroId, token);
                    decimal free = (stock?.EXI_CantidadDisponible ?? 0) - (stock?.EXI_CantidadReservada ?? 0);
                    decimal available = lot is null || lot.LOT_AlmacenId != reception.REC_AlmacenId ? 0 : Math.Min(lot.LOT_CantidadActual, free);

                    if (quantity > available || stock is null || lot is null)
                    {
                        conflicts.Add($"\"{detail.Libro.LIB_Titulo}\": se quieren devolver {quantity:0} y del lote {lot?.LOT_Numero ?? "—"} quedan {Math.Max(available, 0):0} libres en el almacén.");
                        continue;
                    }

                    lot.LOT_CantidadActual -= quantity;
                    lot.LOT_Activo = lot.LOT_CantidadActual > 0;
                    lot.LOT_FechaModificacion = now;

                    stock.EXI_CantidadDisponible -= quantity;
                    stock.EXI_FechaUltimoMovimiento = now;
                    stock.EXI_FechaModificacion = now;

                    movement.Detalles.Add(new PB_MOVIMIENTO_INVENTARIO_DETALLE
                    {
                        MDE_LibroId = detail.RCD_LibroId,
                        Lote = lot,
                        MDE_Cantidad = quantity,
                        MDE_CostoUnitario = lot.LOT_CostoUnitario,
                        MDE_CostoTotal = decimal.Round(quantity * lot.LOT_CostoUnitario, 2, MidpointRounding.AwayFromZero)
                    });
                }

                if (errors.Count > 0)
                {
                    return OperationResult<SupplierReturnResponse>.Failure(errors.ToArray());
                }

                if (conflicts.Count > 0)
                {
                    return OperationResult<SupplierReturnResponse>.Conflict(conflicts.ToArray());
                }

                _context.MovimientosInventario.Add(movement);
                await _context.SaveChangesAsync(token);

                decimal total = movement.Detalles.Sum(line => line.MDE_CostoTotal);

                _audit.Add(
                    AuditActions.Return,
                    Entity,
                    movement.MOV_MovimientoInventario.ToString(),
                    $"Devolución al proveedor {movement.MOV_Numero} de la recepción {reception.REC_Numero}: " +
                    $"{movement.Detalles.Sum(line => line.MDE_Cantidad):0} unidades por Q{total:N2} al costo. Motivo: {movement.MOV_Observaciones}",
                    [AuditChange.Of("Total al costo", null, total)]);

                await _context.SaveChangesAsync(token);

                return OperationResult<SupplierReturnResponse>.Success((await GetByIdAsync(movement.MOV_MovimientoInventario, token))!);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<SupplierReturnResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<SupplierReturnResponse>> AnnulAsync(
        int id,
        AnnulSupplierReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        if (await _employees.FindAsync(authenticatedUserId, cancellationToken) is null)
        {
            return OperationResult<SupplierReturnResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(async token =>
            {
                PB_MOVIMIENTO_INVENTARIO? movement = await _context.MovimientosInventario
                    .Include(item => item.Detalles).ThenInclude(line => line.Lote)
                    .FirstOrDefaultAsync(item => item.MOV_MovimientoInventario == id && item.TipoMovimiento.TMI_Codigo == SupplierReturnType, token);

                if (movement is null)
                {
                    return OperationResult<SupplierReturnResponse>.Missing("La devolución al proveedor no existe.");
                }

                if (movement.MOV_Anulado)
                {
                    return OperationResult<SupplierReturnResponse>.Conflict($"La devolución {movement.MOV_Numero} ya está anulada.");
                }

                DateTime now = DateTime.UtcNow;
                int warehouse = movement.MOV_AlmacenOrigenId!.Value;

                // La mercadería no llegó a salir (o el proveedor la regresó): vuelve a su lote.
                foreach (PB_MOVIMIENTO_INVENTARIO_DETALLE line in movement.Detalles)
                {
                    if (line.Lote is not null)
                    {
                        line.Lote.LOT_CantidadActual += line.MDE_Cantidad;
                        line.Lote.LOT_Activo = true;
                        line.Lote.LOT_FechaModificacion = now;
                    }

                    PB_EXISTENCIA stock = await _context.Existencias.SingleAsync(
                        item => item.EXI_AlmacenId == warehouse && item.EXI_LibroId == line.MDE_LibroId, token);

                    stock.EXI_CantidadDisponible += line.MDE_Cantidad;
                    stock.EXI_FechaUltimoMovimiento = now;
                    stock.EXI_FechaModificacion = now;
                }

                string reason = request.Reason.Trim();
                movement.MOV_Anulado = true;
                movement.MOV_FechaAnulacion = now;
                movement.MOV_MotivoAnulacion = reason;

                _audit.Add(
                    AuditActions.Annul,
                    Entity,
                    movement.MOV_MovimientoInventario.ToString(),
                    $"Anuló la devolución al proveedor {movement.MOV_Numero}; las unidades regresaron al inventario: {reason}");

                await _context.SaveChangesAsync(token);

                return OperationResult<SupplierReturnResponse>.Success((await GetByIdAsync(id, token))!);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<SupplierReturnResponse>.Conflict(ConcurrencyMessage);
        }
    }
}
