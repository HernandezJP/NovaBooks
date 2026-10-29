using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Cash;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Domain.Entities.Payments;
using TikalLibros.Domain.Entities.Sales;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Audit;
using TikalLibros.Infrastructure.Services.Cash;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services.Sales;

/// <summary>
/// Devoluciones de clientes (notas de crédito). El monto de cada línea es
/// proporcional a lo cobrado (con descuento e IVA). Primero se abona al saldo
/// pendiente de la venta al crédito y el resto se reembolsa por la caja
/// abierta. Solo los motivos que reintegran inventario devuelven el libro al
/// almacén de la venta, en un lote con el número de la devolución y el costo
/// de la venta; los demás (dañado, defectuoso) lo dan de baja.
/// </summary>
public sealed class ReturnService : IReturnService
{
    public const string CustomerReturnType = "DEVOLUCION_CLIENTE";

    private const string ReturnEntity = "DevolucionVenta";
    private const string SaleEntity = "Venta";
    private const string FinalConsumer = "Consumidor final";

    private const string ConcurrencyMessage =
        "La venta o el inventario cambiaron mientras se guardaba la devolución. Intente nuevamente.";

    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;
    private readonly CashService _cash;
    private readonly ISystemParameterService _parameters;
    private readonly AuditTrail _audit;

    public ReturnService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees,
        CashService cash,
        ISystemParameterService parameters,
        AuditTrail audit)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
        _cash = cash;
        _parameters = parameters;
        _audit = audit;
    }

    public async Task<ReturnCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default)
    {
        return new ReturnCatalogsResponse
        {
            Reasons = await _context.MotivosDevolucion
                .AsNoTracking()
                .Where(reason => reason.MDV_Activo)
                .OrderBy(reason => reason.MDV_Nombre)
                .Select(reason => new ReturnReasonResponse
                {
                    Id = reason.MDV_MotivoDevolucion,
                    Code = reason.MDV_Codigo,
                    Name = reason.MDV_Nombre,
                    Restocks = reason.MDV_ReintegraInventario,
                    RequiresNotes = reason.MDV_RequiereObservacion
                })
                .ToListAsync(cancellationToken),
            RefundMethods = await _context.MetodosPago
                .AsNoTracking()
                .Where(method => method.MPA_Activo && method.MPA_Codigo != PaymentMethodCode.Credit)
                .OrderByDescending(method => method.MPA_AfectaEfectivo)
                .ThenBy(method => method.MPA_Nombre)
                .Select(method => new CatalogOptionResponse { Id = method.MPA_MetodoPago, Code = method.MPA_Codigo, Name = method.MPA_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PagedResponse<ReturnListItemResponse>> GetPagedAsync(
        ReturnQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_DEVOLUCION_VENTA> returns = _context.DevolucionesVenta.AsNoTracking();

        if (!query.IncludeAnnulled)
        {
            returns = returns.Where(item => !item.DEV_Anulada);
        }

        if (query.SaleId.HasValue)
        {
            returns = returns.Where(item => item.DEV_VentaId == query.SaleId);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            returns = returns.Where(item => item.DEV_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            returns = returns.Where(item => item.DEV_Fecha < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();

            returns = returns.Where(item =>
                item.DEV_Numero.Contains(value) ||
                item.Venta.VEN_Numero.Contains(value) ||
                (item.Venta.Cliente != null &&
                 (item.Venta.Cliente.CLI_Codigo.Contains(value) ||
                  (item.Venta.Cliente.Persona.PersonaJuridica != null && item.Venta.Cliente.Persona.PersonaJuridica.PJU_RazonSocial.Contains(value)) ||
                  (item.Venta.Cliente.Persona.PersonaNatural != null &&
                   (item.Venta.Cliente.Persona.PersonaNatural.PNA_PrimerNombre + " " + item.Venta.Cliente.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value)))));
        }

        int totalItems = await returns.CountAsync(cancellationToken);

        List<ReturnListItemResponse> items = await returns
            .OrderByDescending(item => item.DEV_Fecha)
            .ThenByDescending(item => item.DEV_DevolucionVenta)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ReturnListItemResponse
            {
                Id = item.DEV_DevolucionVenta,
                Number = item.DEV_Numero,
                Date = item.DEV_Fecha,
                SaleId = item.DEV_VentaId,
                SaleNumber = item.Venta.VEN_Numero,
                Customer = item.Venta.Cliente == null
                    ? FinalConsumer
                    : item.Venta.Cliente.Persona.PersonaJuridica != null
                        ? item.Venta.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                        : item.Venta.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Venta.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                Units = item.Detalles.Sum(line => line.DVD_Cantidad),
                Total = item.DEV_Total,
                CreditApplied = item.Reembolsos.Where(refund => refund.MetodoPago.MPA_Codigo == PaymentMethodCode.Credit).Sum(refund => refund.DPA_MontoBase),
                Refunded = item.Reembolsos.Where(refund => refund.MetodoPago.MPA_Codigo != PaymentMethodCode.Credit).Sum(refund => refund.DPA_MontoBase),
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                Annulled = item.DEV_Anulada
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReturnListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ReturnResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ReturnResponse? response = await _context.DevolucionesVenta
            .AsNoTracking()
            .Where(item => item.DEV_DevolucionVenta == id)
            .Select(item => new ReturnResponse
            {
                Id = item.DEV_DevolucionVenta,
                Number = item.DEV_Numero,
                Date = item.DEV_Fecha,
                StatusCode = item.EstadoDevolucion.EDV_Codigo,
                Status = item.EstadoDevolucion.EDV_Nombre,
                SaleId = item.DEV_VentaId,
                SaleNumber = item.Venta.VEN_Numero,
                Customer = item.Venta.Cliente == null
                    ? FinalConsumer
                    : item.Venta.Cliente.Persona.PersonaJuridica != null
                        ? item.Venta.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                        : item.Venta.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Venta.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                CustomerNit = item.Venta.Cliente == null
                    ? "CF"
                    : item.Venta.Cliente.Persona.Identificaciones
                        .Where(identification => identification.PID_Activo && identification.TipoIdentificacion.TID_Codigo == "NIT")
                        .Select(identification => identification.PID_Numero)
                        .FirstOrDefault(),
                Warehouse = item.Almacen.ALM_Nombre,
                CashRegister = item.AperturaCaja != null ? item.AperturaCaja.Caja.CAJ_Nombre : null,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                           " (" + item.Empleado.EMP_Codigo + ")",
                Notes = item.DEV_Observaciones,
                Subtotal = item.DEV_Subtotal,
                Tax = item.DEV_Impuesto,
                Total = item.DEV_Total,
                Annulled = item.DEV_Anulada,
                AnnulledAt = item.DEV_FechaAnulacion,
                AnnulmentReason = item.DEV_MotivoAnulacion,
                Lines = item.Detalles
                    .OrderBy(line => line.DVD_DevolucionVentaDetalle)
                    .Select(line => new ReturnLineResponse
                    {
                        SaleLineId = line.DVD_VentaDetalleId,
                        BookId = line.DVD_LibroId,
                        BookCode = line.Libro.LIB_Codigo,
                        Title = line.Libro.LIB_Titulo,
                        Quantity = line.DVD_Cantidad,
                        UnitPrice = line.DVD_PrecioUnitario,
                        TaxAmount = line.DVD_MontoImpuesto,
                        Total = line.DVD_Total,
                        Reason = line.MotivoDevolucion.MDV_Nombre,
                        Restocks = line.DVD_ReintegraInventario,
                        Notes = line.DVD_Observaciones
                    })
                    .ToList(),
                Refunds = item.Reembolsos
                    .OrderBy(refund => refund.DPA_DevolucionPago)
                    .Select(refund => new ReturnRefundResponse
                    {
                        PaymentMethodCode = refund.MetodoPago.MPA_Codigo,
                        PaymentMethod = refund.MetodoPago.MPA_Codigo == PaymentMethodCode.Credit ? "Abono al saldo de la venta" : refund.MetodoPago.MPA_Nombre,
                        Amount = refund.DPA_MontoBase,
                        Reference = refund.DPA_Referencia,
                        CashMovementNumber = refund.MovimientoCaja != null ? refund.MovimientoCaja.MCA_Numero : null,
                        Annulled = refund.DPA_Anulado
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return null;
        }

        response.CreditApplied = response.Refunds.Where(refund => refund.PaymentMethodCode == PaymentMethodCode.Credit).Sum(refund => refund.Amount);
        response.Refunded = response.Refunds.Where(refund => refund.PaymentMethodCode != PaymentMethodCode.Credit).Sum(refund => refund.Amount);
        response.CanAnnul = !response.Annulled;
        response.InventoryMovementNumber = await _context.MovimientosInventario
            .Where(movement => movement.TipoMovimiento.TMI_Codigo == CustomerReturnType && movement.MOV_DocumentoReferencia == response.Number)
            .Select(movement => movement.MOV_Numero)
            .FirstOrDefaultAsync(cancellationToken);

        return response;
    }

    public async Task<OperationResult<ReturnResponse>> CreateAsync(
        SaveReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<ReturnResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        int returnDays = (int)await _parameters.GetDecimalAsync(SystemParameterKeys.ReturnDays, cancellationToken);

        try
        {
            return await _context.ExecuteInTransactionAsync(async token =>
            {
                PB_VENTA? sale = await _context.Ventas
                    .Include(item => item.Detalles).ThenInclude(line => line.Libro)
                    .Include(item => item.EstadoVenta)
                    .FirstOrDefaultAsync(item => item.VEN_Venta == request.SaleId, token);

                if (sale is null)
                {
                    return OperationResult<ReturnResponse>.Missing("La venta no existe.");
                }

                if (sale.VEN_Anulada)
                {
                    return OperationResult<ReturnResponse>.Conflict($"La venta {sale.VEN_Numero} está anulada; no admite devoluciones.");
                }

                DateOnly saleDate = BusinessTime.ToLocalDate(sale.VEN_Fecha);

                if (BusinessTime.Today > saleDate.AddDays(returnDays))
                {
                    return OperationResult<ReturnResponse>.Conflict(
                        $"La venta {sale.VEN_Numero} es del {saleDate:dd/MM/yyyy}; las devoluciones se aceptan hasta {returnDays} días después de la venta.");
                }

                Dictionary<int, PB_MOTIVO_DEVOLUCION> reasons = await _context.MotivosDevolucion
                    .Where(reason => reason.MDV_Activo)
                    .ToDictionaryAsync(reason => reason.MDV_MotivoDevolucion, token);

                List<string> errors = [];
                List<string> conflicts = [];

                for (int index = 0; index < request.Lines.Count; index++)
                {
                    ReturnLineRequest line = request.Lines[index];
                    PB_VENTA_DETALLE? saleLine = sale.Detalles.FirstOrDefault(item => item.VDE_VentaDetalle == line.SaleLineId);

                    if (saleLine is null)
                    {
                        errors.Add($"Línea {index + 1}: no pertenece a la venta {sale.VEN_Numero}.");
                        continue;
                    }

                    if (!reasons.TryGetValue(line.ReasonId!.Value, out PB_MOTIVO_DEVOLUCION? reason))
                    {
                        errors.Add($"Línea {index + 1}: el motivo no existe o está inactivo.");
                    }
                    else if (reason.MDV_RequiereObservacion && (line.Notes?.Trim().Length ?? 0) < 5)
                    {
                        errors.Add($"Línea {index + 1}: el motivo \"{reason.MDV_Nombre}\" requiere una observación (mínimo 5 caracteres).");
                    }

                    decimal remaining = saleLine.VDE_Cantidad - saleLine.VDE_CantidadDevuelta;

                    if (line.Quantity > remaining)
                    {
                        conflicts.Add($"\"{saleLine.Libro.LIB_Titulo}\": se vendieron {saleLine.VDE_Cantidad:0}, ya se devolvieron {saleLine.VDE_CantidadDevuelta:0} y se quieren devolver {line.Quantity}.");
                    }
                }

                if (errors.Count > 0)
                {
                    return OperationResult<ReturnResponse>.Failure(errors.ToArray());
                }

                if (conflicts.Count > 0)
                {
                    return OperationResult<ReturnResponse>.Conflict(conflicts.ToArray());
                }

                DateTime now = DateTime.UtcNow;

                PB_DEVOLUCION_VENTA entry = new()
                {
                    DEV_Numero = await _numbers.NextAsync(DocumentTypes.SaleReturn, token),
                    DEV_VentaId = sale.VEN_Venta,
                    DEV_EstadoDevolucionId = await StatusIdAsync(ReturnStatus.Processed, token),
                    DEV_AlmacenId = sale.VEN_AlmacenId,
                    DEV_EmpleadoId = employee.Id,
                    DEV_MonedaId = sale.VEN_MonedaId,
                    DEV_Fecha = now,
                    DEV_TipoCambio = 1m,
                    DEV_Observaciones = Clean(request.Notes),
                    DEV_ReembolsoProcesado = true,
                    DEV_FechaProcesado = now,
                    DEV_FechaCreacion = now
                };

                foreach (ReturnLineRequest line in request.Lines)
                {
                    PB_VENTA_DETALLE saleLine = sale.Detalles.Single(item => item.VDE_VentaDetalle == line.SaleLineId);
                    PB_MOTIVO_DEVOLUCION reason = reasons[line.ReasonId!.Value];
                    decimal quantity = line.Quantity!.Value;
                    (decimal total, decimal tax) = await LineAmountsAsync(saleLine, quantity, token);

                    entry.Detalles.Add(new PB_DEVOLUCION_VENTA_DETALLE
                    {
                        DVD_VentaDetalleId = saleLine.VDE_VentaDetalle,
                        DVD_LibroId = saleLine.VDE_LibroId,
                        DVD_MotivoDevolucionId = reason.MDV_MotivoDevolucion,
                        DVD_Cantidad = quantity,
                        DVD_PrecioUnitario = decimal.Round(total / quantity, 2, MidpointRounding.AwayFromZero),
                        DVD_MontoImpuesto = tax,
                        DVD_Total = total,
                        DVD_ReintegraInventario = reason.MDV_ReintegraInventario,
                        DVD_Observaciones = Clean(line.Notes)
                    });

                    saleLine.VDE_CantidadDevuelta += quantity;
                }

                entry.DEV_Total = entry.Detalles.Sum(line => line.DVD_Total);
                entry.DEV_Impuesto = entry.Detalles.Sum(line => line.DVD_MontoImpuesto);
                entry.DEV_Subtotal = entry.DEV_Total - entry.DEV_Impuesto;

                // Primero se abona al saldo pendiente; el resto se reembolsa.
                decimal credit = Math.Min(entry.DEV_Total, Math.Max(sale.VEN_Total - sale.VEN_TotalPagado, 0));
                decimal refund = entry.DEV_Total - credit;

                if (refund > 0)
                {
                    OperationResult<ReturnResponse>? refundError = await AddRefundAsync(entry, sale, request, employee.Id, refund, now, token);

                    if (refundError is not null)
                    {
                        return refundError;
                    }
                }

                if (credit > 0)
                {
                    entry.Reembolsos.Add(new PB_DEVOLUCION_PAGO
                    {
                        DPA_MetodoPagoId = await MethodIdAsync(PaymentMethodCode.Credit, token),
                        DPA_MonedaId = sale.VEN_MonedaId,
                        DPA_Fecha = now,
                        DPA_TipoCambio = 1m,
                        DPA_MontoMoneda = credit,
                        DPA_MontoBase = credit,
                        DPA_Referencia = $"Abono al saldo de la venta {sale.VEN_Numero}"
                    });

                    sale.VEN_TotalPagado += credit;
                }

                entry.DEV_InventarioProcesado = await RestockAsync(entry, sale, employee.Id, now, token);

                string stateBefore = sale.EstadoVenta.EVE_Nombre;
                sale.VEN_EstadoVentaId = await StateIdAsync(sale, token);
                sale.VEN_FechaModificacion = now;

                // Si hubo reembolso, la numeración del movimiento de inventario ya la
                // guardó junto con el movimiento de caja (dentro de la transacción).
                if (_context.Entry(entry).State == EntityState.Detached)
                {
                    _context.DevolucionesVenta.Add(entry);
                }

                await _context.SaveChangesAsync(token);

                string restocked = entry.Detalles.Any(line => line.DVD_ReintegraInventario) ? "; la mercadería en buen estado volvió al inventario" : "";

                _audit.Add(
                    AuditActions.Return,
                    ReturnEntity,
                    entry.DEV_DevolucionVenta.ToString(),
                    $"Devolución {entry.DEV_Numero} de la venta {sale.VEN_Numero} por Q{entry.DEV_Total:N2}: " +
                    $"abono al saldo Q{credit:N2}, reembolso Q{refund:N2}{restocked}.",
                    [
                        AuditChange.Of("Total", null, entry.DEV_Total),
                        AuditChange.Of("Unidades", null, entry.Detalles.Sum(line => line.DVD_Cantidad))
                    ]);

                _audit.Add(
                    AuditActions.Return,
                    SaleEntity,
                    sale.VEN_Venta.ToString(),
                    $"Se registró la devolución {entry.DEV_Numero} por Q{entry.DEV_Total:N2}.",
                    [AuditChange.Of("Estado", stateBefore, await StatusNameAsync(sale.VEN_EstadoVentaId, token))]);

                await _context.SaveChangesAsync(token);

                return OperationResult<ReturnResponse>.Success((await GetByIdAsync(entry.DEV_DevolucionVenta, token))!);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ReturnResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<ReturnResponse>> AnnulAsync(
        int id,
        AnnulReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        if (await _employees.FindAsync(authenticatedUserId, cancellationToken) is null)
        {
            return OperationResult<ReturnResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(async token =>
            {
                PB_DEVOLUCION_VENTA? entry = await _context.DevolucionesVenta
                    .Include(item => item.Detalles).ThenInclude(line => line.VentaDetalle)
                    .Include(item => item.Detalles).ThenInclude(line => line.Libro)
                    .Include(item => item.Reembolsos).ThenInclude(refund => refund.MetodoPago)
                    .Include(item => item.Reembolsos).ThenInclude(refund => refund.MovimientoCaja!).ThenInclude(movement => movement.AperturaCaja)
                    .Include(item => item.Venta).ThenInclude(sale => sale.EstadoVenta)
                    .Include(item => item.Venta).ThenInclude(sale => sale.Detalles)
                    .FirstOrDefaultAsync(item => item.DEV_DevolucionVenta == id, token);

                if (entry is null)
                {
                    return OperationResult<ReturnResponse>.Missing("La devolución no existe.");
                }

                if (entry.DEV_Anulada)
                {
                    return OperationResult<ReturnResponse>.Conflict($"La devolución {entry.DEV_Numero} ya está anulada.");
                }

                if (entry.Reembolsos.Any(refund => !refund.DPA_Anulado && refund.MovimientoCaja is { AperturaCaja.ACA_Abierta: false }))
                {
                    return OperationResult<ReturnResponse>.Conflict(
                        $"El reembolso de la devolución {entry.DEV_Numero} ya entró en un cierre de caja; no puede anularse.");
                }

                DateTime now = DateTime.UtcNow;
                string reason = request.Reason.Trim();

                // Inventario: se retira lo que había entrado con la devolución.
                PB_MOVIMIENTO_INVENTARIO? movement = await _context.MovimientosInventario
                    .Include(item => item.Detalles).ThenInclude(line => line.Lote)
                    .FirstOrDefaultAsync(item => item.TipoMovimiento.TMI_Codigo == CustomerReturnType &&
                                                 item.MOV_DocumentoReferencia == entry.DEV_Numero &&
                                                 !item.MOV_Anulado, token);

                if (movement is not null)
                {
                    List<string> blocked = [];

                    foreach (PB_MOVIMIENTO_INVENTARIO_DETALLE line in movement.Detalles)
                    {
                        PB_EXISTENCIA stock = await _context.Existencias.SingleAsync(
                            item => item.EXI_AlmacenId == entry.DEV_AlmacenId && item.EXI_LibroId == line.MDE_LibroId, token);
                        decimal free = stock.EXI_CantidadDisponible - stock.EXI_CantidadReservada;

                        if (line.Lote is null || line.Lote.LOT_CantidadActual < line.MDE_Cantidad || free < line.MDE_Cantidad)
                        {
                            string title = entry.Detalles.First(detail => detail.DVD_LibroId == line.MDE_LibroId).Libro.LIB_Titulo;
                            blocked.Add($"\"{title}\": las unidades devueltas ya se vendieron, reservaron o movieron.");
                            continue;
                        }

                        line.Lote.LOT_CantidadActual -= line.MDE_Cantidad;
                        line.Lote.LOT_Activo = line.Lote.LOT_CantidadActual > 0;
                        line.Lote.LOT_FechaModificacion = now;
                        stock.EXI_CantidadDisponible -= line.MDE_Cantidad;
                        stock.EXI_FechaUltimoMovimiento = now;
                        stock.EXI_FechaModificacion = now;
                    }

                    if (blocked.Count > 0)
                    {
                        return OperationResult<ReturnResponse>.Conflict(blocked.ToArray());
                    }

                    movement.MOV_Anulado = true;
                    movement.MOV_FechaAnulacion = now;
                    movement.MOV_MotivoAnulacion = reason;
                }

                PB_VENTA sale = entry.Venta;
                string stateBefore = sale.EstadoVenta.EVE_Nombre;

                foreach (PB_DEVOLUCION_PAGO refund in entry.Reembolsos.Where(refund => !refund.DPA_Anulado))
                {
                    refund.DPA_Anulado = true;

                    if (refund.MetodoPago.MPA_Codigo == PaymentMethodCode.Credit)
                    {
                        // El abono al saldo se revierte: la venta vuelve a deber ese monto.
                        sale.VEN_TotalPagado -= refund.DPA_MontoBase;
                    }

                    if (refund.MovimientoCaja is { MCA_Anulado: false } cashMovement)
                    {
                        cashMovement.MCA_Anulado = true;
                        cashMovement.MCA_FechaAnulacion = now;
                        cashMovement.MCA_MotivoAnulacion = $"Anulación de la devolución {entry.DEV_Numero}: {reason}";
                    }
                }

                foreach (PB_DEVOLUCION_VENTA_DETALLE line in entry.Detalles)
                {
                    line.VentaDetalle.VDE_CantidadDevuelta -= line.DVD_Cantidad;
                }

                entry.DEV_Anulada = true;
                entry.DEV_FechaAnulacion = now;
                entry.DEV_MotivoAnulacion = reason;
                entry.DEV_EstadoDevolucionId = await StatusIdAsync(ReturnStatus.Annulled, token);

                sale.VEN_EstadoVentaId = await StateIdAsync(sale, token);
                sale.VEN_FechaModificacion = now;

                _audit.Add(
                    AuditActions.Annul,
                    ReturnEntity,
                    entry.DEV_DevolucionVenta.ToString(),
                    $"Anuló la devolución {entry.DEV_Numero} de la venta {sale.VEN_Numero} por Q{entry.DEV_Total:N2}; " +
                    $"el inventario, el saldo y el reembolso se revirtieron: {reason}",
                    [AuditChange.Of("Estado de la venta", stateBefore, await StatusNameAsync(sale.VEN_EstadoVentaId, token))]);

                await _context.SaveChangesAsync(token);

                return OperationResult<ReturnResponse>.Success((await GetByIdAsync(id, token))!);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ReturnResponse>.Conflict(ConcurrencyMessage);
        }
    }

    /// <summary>
    /// Monto proporcional a lo cobrado en la línea. Al devolver las últimas
    /// unidades se toma el resto exacto para que no queden centavos sueltos.
    /// </summary>
    private async Task<(decimal Total, decimal Tax)> LineAmountsAsync(PB_VENTA_DETALLE saleLine, decimal quantity, CancellationToken cancellationToken)
    {
        if (saleLine.VDE_CantidadDevuelta + quantity >= saleLine.VDE_Cantidad)
        {
            var previous = await _context.DevolucionesVentaDetalles
                .Where(line => line.DVD_VentaDetalleId == saleLine.VDE_VentaDetalle && !line.DevolucionVenta.DEV_Anulada)
                .GroupBy(_ => 1)
                .Select(group => new { Total = group.Sum(line => line.DVD_Total), Tax = group.Sum(line => line.DVD_MontoImpuesto) })
                .FirstOrDefaultAsync(cancellationToken);

            return (saleLine.VDE_Total - (previous?.Total ?? 0), saleLine.VDE_MontoImpuesto - (previous?.Tax ?? 0));
        }

        return (Round(saleLine.VDE_Total * quantity / saleLine.VDE_Cantidad), Round(saleLine.VDE_MontoImpuesto * quantity / saleLine.VDE_Cantidad));
    }

    /// <summary>Reembolso por la caja abierta de quien registra la devolución.</summary>
    private async Task<OperationResult<ReturnResponse>?> AddRefundAsync(
        PB_DEVOLUCION_VENTA entry,
        PB_VENTA sale,
        SaveReturnRequest request,
        int employeeId,
        decimal amount,
        DateTime now,
        CancellationToken cancellationToken)
    {
        PB_APERTURA_CAJA? session = await _cash.FindOpenSessionAsync(employeeId, cancellationToken);

        if (session is null)
        {
            return OperationResult<ReturnResponse>.Conflict(
                $"Hay que reembolsar Q{amount:N2} y usted no tiene una caja abierta. Abra su caja para registrar la devolución.");
        }

        PB_METODO_PAGO? method = request.RefundMethodId.HasValue
            ? await _context.MetodosPago.FirstOrDefaultAsync(item => item.MPA_MetodoPago == request.RefundMethodId && item.MPA_Activo, cancellationToken)
            : await _context.MetodosPago.FirstOrDefaultAsync(item => item.MPA_Codigo == PaymentMethodCode.Cash, cancellationToken);

        if (method is null || method.MPA_Codigo == PaymentMethodCode.Credit)
        {
            return OperationResult<ReturnResponse>.Failure("El método de reembolso no existe o no está disponible.");
        }

        if (method.MPA_AfectaEfectivo)
        {
            decimal available = await _cash.ExpectedCashAsync(session, cancellationToken);

            if (amount > available)
            {
                return OperationResult<ReturnResponse>.Conflict(
                    $"No hay suficiente efectivo en caja para el reembolso: hay Q{available:N2} y se deben devolver Q{amount:N2}.");
            }
        }

        PB_TIPO_MOVIMIENTO_CAJA type = await _context.TiposMovimientoCaja
            .SingleAsync(item => item.TMC_Codigo == CashMovementTypes.Refund, cancellationToken);

        PB_MOVIMIENTO_CAJA movement = await _cash.AddMovementAsync(
            session, type, method.MPA_MetodoPago, employeeId, amount,
            $"Devolución {entry.DEV_Numero} de la venta {sale.VEN_Numero}", Clean(request.RefundReference), saleId: null, cancellationToken);

        movement.DevolucionVenta = entry;
        entry.DEV_AperturaCajaId = session.ACA_AperturaCaja;

        entry.Reembolsos.Add(new PB_DEVOLUCION_PAGO
        {
            DPA_MetodoPagoId = method.MPA_MetodoPago,
            DPA_MonedaId = session.ACA_MonedaId,
            DPA_Fecha = now,
            DPA_TipoCambio = 1m,
            DPA_MontoMoneda = amount,
            DPA_MontoBase = amount,
            DPA_Referencia = Clean(request.RefundReference),
            MovimientoCaja = movement
        });

        return null;
    }

    /// <summary>
    /// Entrada al almacén de la venta de lo que vuelve en buen estado: un lote
    /// por libro con el número de la devolución y el costo de la venta.
    /// </summary>
    private async Task<bool> RestockAsync(PB_DEVOLUCION_VENTA entry, PB_VENTA sale, int employeeId, DateTime now, CancellationToken cancellationToken)
    {
        var books = entry.Detalles
            .Where(line => line.DVD_ReintegraInventario)
            .GroupBy(line => line.DVD_LibroId)
            .Select(group => new
            {
                BookId = group.Key,
                Quantity = group.Sum(line => line.DVD_Cantidad),
                Cost = group.Sum(line => line.DVD_Cantidad * sale.Detalles.Single(detail => detail.VDE_VentaDetalle == line.DVD_VentaDetalleId).VDE_CostoUnitario)
            })
            .ToList();

        if (books.Count == 0)
        {
            return false;
        }

        PB_MOVIMIENTO_INVENTARIO movement = new()
        {
            MOV_Numero = await _numbers.NextAsync(DocumentTypes.InventoryMovement, cancellationToken),
            MOV_TipoMovimientoId = await _context.TiposMovimientoInventario
                .Where(type => type.TMI_Codigo == CustomerReturnType)
                .Select(type => type.TMI_TipoMovimientoInventario)
                .SingleAsync(cancellationToken),
            MOV_AlmacenDestinoId = sale.VEN_AlmacenId,
            MOV_EmpleadoId = employeeId,
            MOV_Fecha = now,
            MOV_DocumentoReferencia = entry.DEV_Numero,
            MOV_Observaciones = $"Devolución de la venta {sale.VEN_Numero}",
            MOV_Procesado = true,
            MOV_FechaProcesado = now,
            MOV_FechaCreacion = now
        };

        foreach (var book in books)
        {
            decimal unitCost = decimal.Round(book.Cost / book.Quantity, 4, MidpointRounding.AwayFromZero);

            PB_LOTE lot = new()
            {
                LOT_AlmacenId = sale.VEN_AlmacenId,
                LOT_LibroId = book.BookId,
                LOT_Numero = entry.DEV_Numero,
                LOT_FechaIngreso = BusinessTime.Today,
                LOT_CantidadInicial = book.Quantity,
                LOT_CantidadActual = book.Quantity,
                LOT_CostoUnitario = unitCost,
                LOT_Activo = true,
                LOT_FechaCreacion = now
            };

            movement.Detalles.Add(new PB_MOVIMIENTO_INVENTARIO_DETALLE
            {
                MDE_LibroId = book.BookId,
                Lote = lot,
                MDE_Cantidad = book.Quantity,
                MDE_CostoUnitario = unitCost,
                MDE_CostoTotal = Round(book.Quantity * unitCost)
            });

            PB_EXISTENCIA? stock = await _context.Existencias.FirstOrDefaultAsync(
                item => item.EXI_AlmacenId == sale.VEN_AlmacenId && item.EXI_LibroId == book.BookId, cancellationToken);

            if (stock is null)
            {
                stock = new PB_EXISTENCIA { EXI_AlmacenId = sale.VEN_AlmacenId, EXI_LibroId = book.BookId, EXI_FechaCreacion = now };
                _context.Existencias.Add(stock);
            }

            stock.EXI_CantidadDisponible += book.Quantity;
            stock.EXI_FechaUltimoMovimiento = now;
            stock.EXI_FechaModificacion = now;
        }

        _context.MovimientosInventario.Add(movement);
        return true;
    }

    /// <summary>Devuelta si ya no quedan unidades; si no, según lo pagado.</summary>
    private async Task<int> StateIdAsync(PB_VENTA sale, CancellationToken cancellationToken)
    {
        string code = sale.Detalles.All(line => line.VDE_CantidadDevuelta >= line.VDE_Cantidad)
            ? SaleStatus.Returned
            : sale.VEN_TotalPagado >= sale.VEN_Total
                ? SaleStatus.Paid
                : sale.VEN_TotalPagado > 0 ? SaleStatus.Partial : SaleStatus.Credit;

        return await _context.EstadosVenta
            .Where(status => status.EVE_Codigo == code)
            .Select(status => status.EVE_EstadoVenta)
            .SingleAsync(cancellationToken);
    }

    private Task<string> StatusNameAsync(int id, CancellationToken cancellationToken) =>
        _context.EstadosVenta.Where(status => status.EVE_EstadoVenta == id).Select(status => status.EVE_Nombre).SingleAsync(cancellationToken);

    private Task<int> StatusIdAsync(string code, CancellationToken cancellationToken) =>
        _context.EstadosDevolucion.Where(status => status.EDV_Codigo == code).Select(status => status.EDV_EstadoDevolucion).SingleAsync(cancellationToken);

    private Task<int> MethodIdAsync(string code, CancellationToken cancellationToken) =>
        _context.MetodosPago.Where(method => method.MPA_Codigo == code).Select(method => method.MPA_MetodoPago).SingleAsync(cancellationToken);

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
