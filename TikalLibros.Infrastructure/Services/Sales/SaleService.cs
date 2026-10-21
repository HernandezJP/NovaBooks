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
using TikalLibros.Infrastructure.Services.Inventory;
using TikalLibros.Infrastructure.Services.People;
using static TikalLibros.Infrastructure.Services.Sales.OrderService;

namespace TikalLibros.Infrastructure.Services.Sales;

/// <summary>
/// Ventas: factura un pedido reservado o vende en mostrador. En una sola
/// transacción registra la venta y sus pagos, los movimientos de caja, la
/// salida de inventario (lotes PEPS) y, si viene de un pedido, libera la
/// reserva y lo marca facturado. Anular revierte todo eso.
/// </summary>
public sealed class SaleService : ISaleService
{
    public const string SaleExitType = "SALIDA_VENTA";

    private const string SaleEntity = "Venta";
    private const string FinalConsumer = "Consumidor final";
    private const string NitCode = "NIT";

    private const string ConcurrencyMessage =
        "La existencia cambió mientras se guardaba la venta. Intente nuevamente.";

    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;
    private readonly OrderService _orders;
    private readonly CashService _cash;
    private readonly InventoryService _inventory;
    private readonly InvoiceService _invoices;
    private readonly AuditTrail _audit;

    public SaleService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees,
        OrderService orders,
        CashService cash,
        InventoryService inventory,
        InvoiceService invoices,
        AuditTrail audit)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
        _orders = orders;
        _cash = cash;
        _inventory = inventory;
        _invoices = invoices;
        _audit = audit;
    }

    // ───────────────────────────── Consultas ─────────────────────────────

    public async Task<SaleCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default)
    {
        return new SaleCatalogsResponse
        {
            PaymentMethods = await _context.MetodosPago
                .AsNoTracking()
                .Where(method => method.MPA_Activo && method.MPA_Codigo != PaymentMethodCode.Credit)
                .OrderBy(method => method.MPA_MetodoPago)
                .Select(method => new PaymentMethodResponse
                {
                    Id = method.MPA_MetodoPago,
                    Code = method.MPA_Codigo,
                    Name = method.MPA_Nombre,
                    RequiresReference = method.MPA_RequiereReferencia,
                    RequiresAuthorization = method.MPA_RequiereAutorizacion,
                    AllowsChange = method.MPA_PermiteCambio
                })
                .ToListAsync(cancellationToken),
            Statuses = await _context.EstadosVenta
                .AsNoTracking()
                .Where(state => state.EVE_Activo && state.EVE_Codigo != "BORRADOR" && state.EVE_Codigo != "PENDIENTE")
                .OrderBy(state => state.EVE_EstadoVenta)
                .Select(state => new CatalogOptionResponse { Id = state.EVE_EstadoVenta, Code = state.EVE_Codigo, Name = state.EVE_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PagedResponse<SaleListItemResponse>> GetPagedAsync(
        SaleQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_VENTA> sales = _context.Ventas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            string status = query.Status.Trim().ToUpperInvariant();
            sales = sales.Where(item => item.EstadoVenta.EVE_Codigo == status);
        }

        if (!string.IsNullOrWhiteSpace(query.SaleType))
        {
            string saleType = query.SaleType.Trim().ToUpperInvariant();
            sales = sales.Where(item => item.TipoVenta.TVE_Codigo == saleType);
        }

        if (query.CustomerId.HasValue)
        {
            sales = sales.Where(item => item.VEN_ClienteId == query.CustomerId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            sales = sales.Where(item => item.VEN_EmpleadoId == query.EmployeeId.Value);
        }

        if (query.BookId.HasValue)
        {
            sales = sales.Where(item => item.Detalles.Any(line => line.VDE_LibroId == query.BookId.Value));
        }

        if (query.PaymentMethodId.HasValue)
        {
            sales = sales.Where(item => item.Pagos.Any(payment => !payment.VPA_Anulado && payment.VPA_MetodoPagoId == query.PaymentMethodId.Value));
        }

        if (query.WithBalance == true)
        {
            sales = sales.Where(item => !item.VEN_Anulada && item.VEN_TotalPagado < item.VEN_Total);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            sales = sales.Where(item => item.VEN_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            sales = sales.Where(item => item.VEN_Fecha < to);
        }

        if (query.MinTotal.HasValue)
        {
            sales = sales.Where(item => item.VEN_Total >= query.MinTotal.Value);
        }

        if (query.MaxTotal.HasValue)
        {
            sales = sales.Where(item => item.VEN_Total <= query.MaxTotal.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();

            sales = sales.Where(item =>
                item.VEN_Numero.Contains(value) ||
                (item.PedidoCliente != null && item.PedidoCliente.PED_Numero.Contains(value)) ||
                (item.Factura != null && (item.Factura.FAC_Serie == value || item.Factura.FAC_Numero.ToString() == value)) ||
                (item.Cliente != null &&
                 (item.Cliente.CLI_Codigo.Contains(value) ||
                  (item.Cliente.Persona.PersonaJuridica != null && item.Cliente.Persona.PersonaJuridica.PJU_RazonSocial.Contains(value)) ||
                  (item.Cliente.Persona.PersonaNatural != null &&
                   (item.Cliente.Persona.PersonaNatural.PNA_PrimerNombre + " " + item.Cliente.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value)) ||
                  item.Cliente.Persona.Identificaciones.Any(identification => identification.PID_Activo && identification.PID_Numero.Contains(value)))));
        }

        int totalItems = await sales.CountAsync(cancellationToken);

        List<SaleListItemResponse> items = await sales
            .OrderByDescending(item => item.VEN_Fecha)
            .ThenByDescending(item => item.VEN_Venta)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new SaleListItemResponse
            {
                Id = item.VEN_Venta,
                Number = item.VEN_Numero,
                Date = item.VEN_Fecha,
                CustomerId = item.VEN_ClienteId,
                Customer = item.Cliente == null
                    ? FinalConsumer
                    : item.Cliente.Persona.PersonaJuridica != null
                        ? item.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                        : item.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                SaleTypeCode = item.TipoVenta.TVE_Codigo,
                SaleType = item.TipoVenta.TVE_Nombre,
                StatusCode = item.EstadoVenta.EVE_Codigo,
                Status = item.EstadoVenta.EVE_Nombre,
                Total = item.VEN_Total,
                Paid = item.VEN_TotalPagado,
                Balance = item.VEN_Anulada ? 0 : item.VEN_Total - item.VEN_TotalPagado,
                OrderNumber = item.PedidoCliente != null ? item.PedidoCliente.PED_Numero : null,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                Annulled = item.VEN_Anulada
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<SaleListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<SaleResponse?> GetByIdAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        SaleResponse? response = await _context.Ventas
            .AsNoTracking()
            .Where(item => item.VEN_Venta == id)
            .Select(item => new SaleResponse
            {
                Id = item.VEN_Venta,
                Number = item.VEN_Numero,
                Date = item.VEN_Fecha,
                CustomerId = item.VEN_ClienteId,
                Customer = item.Cliente == null
                    ? FinalConsumer
                    : item.Cliente.Persona.PersonaJuridica != null
                        ? item.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                        : item.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                CustomerNit = item.Cliente == null
                    ? "CF"
                    : item.Cliente.Persona.Identificaciones
                        .Where(identification => identification.PID_Activo && identification.TipoIdentificacion.TID_Codigo == NitCode)
                        .Select(identification => identification.PID_Numero)
                        .FirstOrDefault(),
                OrderId = item.VEN_PedidoClienteId,
                OrderNumber = item.PedidoCliente != null ? item.PedidoCliente.PED_Numero : null,
                Warehouse = item.Almacen.ALM_Nombre,
                CashRegister = item.AperturaCaja != null ? item.AperturaCaja.Caja.CAJ_Nombre : null,
                SaleTypeCode = item.TipoVenta.TVE_Codigo,
                SaleType = item.TipoVenta.TVE_Nombre,
                StatusCode = item.EstadoVenta.EVE_Codigo,
                Status = item.EstadoVenta.EVE_Nombre,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                           " (" + item.Empleado.EMP_Codigo + ")",
                Notes = item.VEN_Observaciones,
                Subtotal = item.VEN_Subtotal,
                Discount = item.VEN_Descuento,
                Tax = item.VEN_Impuesto,
                Total = item.VEN_Total,
                Paid = item.VEN_TotalPagado,
                Balance = item.VEN_Anulada ? 0 : item.VEN_Total - item.VEN_TotalPagado,
                Change = item.VEN_CambioEntregado,
                CreditDueDate = item.VEN_FechaVencimientoCredito,
                Annulled = item.VEN_Anulada,
                AnnulledAt = item.VEN_FechaAnulacion,
                AnnulmentReason = item.VEN_MotivoAnulacion,
                Invoice = item.Factura == null ? null : new InvoiceSummaryResponse
                {
                    Series = item.Factura.FAC_Serie,
                    Number = item.Factura.FAC_Numero,
                    Authorization = item.Factura.FAC_NumeroAutorizacion.ToString(),
                    IssuedAt = item.Factura.FAC_FechaEmision,
                    IsDemo = item.Factura.FAC_EsDemostracion,
                    Annulled = item.Factura.FAC_Anulada
                },
                Lines = item.Detalles
                    .OrderBy(line => line.VDE_NumeroLinea)
                    .Select(line => new SaleLineResponse
                    {
                        BookId = line.VDE_LibroId,
                        BookCode = line.Libro.LIB_Codigo,
                        Title = line.Libro.LIB_Titulo,
                        Quantity = line.VDE_Cantidad,
                        UnitPrice = line.VDE_PrecioUnitario,
                        DiscountPercent = line.VDE_PorcentajeDescuento,
                        DiscountAmount = line.VDE_MontoDescuento,
                        TaxAmount = line.VDE_MontoImpuesto,
                        Subtotal = line.VDE_Subtotal,
                        Total = line.VDE_Total,
                        UnitCost = line.VDE_CostoUnitario
                    })
                    .ToList(),
                Payments = item.Pagos
                    .OrderBy(payment => payment.VPA_VentaPago)
                    .Select(payment => new SalePaymentResponse
                    {
                        Id = payment.VPA_VentaPago,
                        Date = payment.VPA_Fecha,
                        PaymentMethodCode = payment.MetodoPago.MPA_Codigo,
                        PaymentMethod = payment.MetodoPago.MPA_Nombre,
                        Amount = payment.VPA_MontoBase,
                        Received = payment.VPA_MontoRecibido,
                        Change = payment.VPA_CambioEntregado,
                        Reference = payment.VPA_Referencia,
                        Authorization = payment.VPA_Autorizacion,
                        LastFourDigits = payment.VPA_UltimosCuatroDigitos,
                        CashMovementNumber = payment.MovimientoCaja != null ? payment.MovimientoCaja.MCA_Numero : null,
                        Annulled = payment.VPA_Anulado
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return null;
        }

        response.InventoryMovementNumber = await _context.MovimientosInventario
            .Where(movement => movement.TipoMovimiento.TMI_Codigo == SaleExitType && movement.MOV_DocumentoReferencia == response.Number)
            .Select(movement => movement.MOV_Numero)
            .FirstOrDefaultAsync(cancellationToken);

        if (!includeCosts)
        {
            foreach (SaleLineResponse line in response.Lines)
            {
                line.UnitCost = null;
            }
        }

        if (response.Invoice is not null)
        {
            response.Invoice.Authorization = response.Invoice.Authorization.ToUpperInvariant();
        }

        response.CanPay = !response.Annulled && response.Balance > 0;
        response.CanAnnul = !response.Annulled;

        return response;
    }

    // ───────────────────────────── Registro ─────────────────────────────

    public async Task<OperationResult<SaleResponse>> InvoiceOrderAsync(
        int orderId,
        InvoiceOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        await _orders.ExpireReservationsAsync(cancellationToken);

        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<SaleResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_PEDIDO_CLIENTE? order = await _orders.LoadAsync(orderId, token);

                    if (order is null)
                    {
                        return OperationResult<SaleResponse>.Missing("El pedido no existe.");
                    }

                    if (order.EstadoPedido.EPE_Codigo != OrderStatus.Reserved)
                    {
                        return OperationResult<SaleResponse>.Conflict(
                            $"Solo se facturan pedidos reservados; el pedido {order.PED_Numero} está {order.EstadoPedido.EPE_Nombre.ToLowerInvariant()}.");
                    }

                    var customer = await _context.Clientes
                        .Where(item => item.CLI_Cliente == order.PED_ClienteId)
                        .Select(item => new { item.CLI_DiasCredito })
                        .SingleAsync(token);

                    string saleTypeCode = order.TipoVenta?.TVE_Codigo ?? SaleType.Cash;
                    int saleTypeId = order.PED_TipoVentaId ?? await _context.TiposVenta
                        .Where(item => item.TVE_Codigo == SaleType.Cash)
                        .Select(item => item.TVE_TipoVenta)
                        .SingleAsync(token);

                    SaleDraft draft = new(
                        order.PED_ClienteId,
                        customer.CLI_DiasCredito,
                        saleTypeId,
                        saleTypeCode,
                        order.PED_AlmacenId,
                        order.PED_SucursalId,
                        order.Detalles
                            .OrderBy(line => line.PDD_NumeroLinea)
                            .Select(line => new PricedLine(
                                line.PDD_LibroId,
                                line.Libro.LIB_Titulo,
                                line.PDD_Cantidad,
                                line.PDD_PrecioUnitario,
                                line.PDD_PorcentajeDescuento,
                                line.PDD_PorcentajeImpuesto,
                                new SaleLineAmounts(line.PDD_Subtotal, line.PDD_MontoDescuento, line.PDD_MontoImpuesto, line.PDD_Total)))
                            .ToList());

                    // La mercadería ya estaba apartada para este pedido: se libera
                    // la reserva y se descuenta la existencia.
                    // Se guarda dentro de la transacción para que la validación de
                    // existencia la vea; si la venta falla, se revierte.
                    await _orders.ReleaseAsync(order, token);
                    await _context.SaveChangesAsync(token);

                    OperationResult<PB_VENTA> sale = await RegisterAsync(draft, request.Payments, Clean(request.Notes), order.PED_PedidoCliente, employee, approvedBy: null, token);

                    if (!sale.Succeeded)
                    {
                        return sale.As<SaleResponse>();
                    }

                    foreach (PB_PEDIDO_CLIENTE_DETALLE line in order.Detalles)
                    {
                        line.PDD_CantidadFacturada = line.PDD_Cantidad;
                    }

                    order.PED_EstadoPedidoId = await _orders.StateIdAsync(OrderStatus.Invoiced, token);
                    order.PED_FechaModificacion = DateTime.UtcNow;

                    _audit.Add(
                        AuditActions.Sell,
                        SaleEntity,
                        sale.Data!.VEN_Venta.ToString(),
                        $"Facturó el pedido {order.PED_Numero} en la venta {sale.Data.VEN_Numero} por Q{sale.Data.VEN_Total:N2}.",
                        SaleChanges(sale.Data));

                    await _context.SaveChangesAsync(token);

                    return OperationResult<SaleResponse>.Success((await GetByIdAsync(sale.Data.VEN_Venta, true, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<SaleResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<SaleResponse>> SellAsync(
        DirectSaleRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        await _orders.ExpireReservationsAsync(cancellationToken);

        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<SaleResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        OperationResult<SaleDraft> prepared = await _orders.PrepareAsync(
            request.CustomerId, request.SaleType, request.PriceListId, request.WarehouseId, request.Lines, permissions, cancellationToken);

        if (!prepared.Succeeded)
        {
            return prepared.As<SaleResponse>();
        }

        SaleDraft draft = prepared.Data!;
        decimal paid = request.Payments.Sum(payment => payment.Amount ?? 0m);

        // Lo que en un pedido requeriría aprobación solo lo vende directamente
        // quien puede aprobar; los demás deben registrar un pedido.
        IReadOnlyCollection<string> reasons = await _orders.ApprovalReasonsAsync(
            draft.HighestDiscount, draft.IsCredit, draft.CustomerId, Math.Max(draft.Total - paid, 0m), null, cancellationToken);

        if (reasons.Count > 0 && !permissions.CanApprove)
        {
            return OperationResult<SaleResponse>.Conflict(
                [.. reasons, "Registre un pedido para que un supervisor lo apruebe, o pida a un supervisor que haga la venta."]);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    OperationResult<PB_VENTA> sale = await RegisterAsync(
                        draft, request.Payments, Clean(request.Notes), orderId: null, employee,
                        approvedBy: reasons.Count > 0 ? employee : null, token);

                    if (!sale.Succeeded)
                    {
                        return sale.As<SaleResponse>();
                    }

                    _audit.Add(
                        AuditActions.Sell,
                        SaleEntity,
                        sale.Data!.VEN_Venta.ToString(),
                        $"Venta de mostrador {sale.Data.VEN_Numero} por Q{sale.Data.VEN_Total:N2}" +
                        (reasons.Count > 0 ? $"; autorizada por {employee.Name} ({employee.Code}): {string.Join(" ", reasons)}" : "."),
                        SaleChanges(sale.Data));

                    await _context.SaveChangesAsync(token);

                    return OperationResult<SaleResponse>.Success((await GetByIdAsync(sale.Data.VEN_Venta, true, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<SaleResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<SaleResponse>> AddPaymentsAsync(
        int saleId,
        AddPaymentsRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<SaleResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                PB_VENTA? sale = await _context.Ventas
                    .Include(item => item.EstadoVenta)
                    .FirstOrDefaultAsync(item => item.VEN_Venta == saleId, token);

                if (sale is null)
                {
                    return OperationResult<SaleResponse>.Missing("La venta no existe.");
                }

                if (sale.VEN_Anulada)
                {
                    return OperationResult<SaleResponse>.Conflict($"La venta {sale.VEN_Numero} está anulada.");
                }

                decimal balance = sale.VEN_Total - sale.VEN_TotalPagado;

                if (balance <= 0)
                {
                    return OperationResult<SaleResponse>.Conflict($"La venta {sale.VEN_Numero} no tiene saldo pendiente.");
                }

                PB_APERTURA_CAJA? session = await _cash.FindOpenSessionAsync(employee.Id, token);

                if (session is null)
                {
                    return OperationResult<SaleResponse>.Conflict(CashService.NoOpenSessionMessage);
                }

                OperationResult<List<ResolvedPayment>> payments = await ResolvePaymentsAsync(request.Payments, balance, requireFull: false, token);

                if (!payments.Succeeded)
                {
                    return payments.As<SaleResponse>();
                }

                string stateBefore = sale.EstadoVenta.EVE_Nombre;
                decimal paidBefore = sale.VEN_TotalPagado;

                await AddPaymentsAsync(sale, session, payments.Data!, employee.Id, token);
                await UpdateStatusAsync(sale, token);
                sale.VEN_FechaModificacion = DateTime.UtcNow;

                _audit.Add(
                    AuditActions.Pay,
                    SaleEntity,
                    sale.VEN_Venta.ToString(),
                    $"Abono de Q{payments.Data!.Sum(item => item.Amount):N2} a la venta {sale.VEN_Numero}; saldo Q{sale.VEN_Total - sale.VEN_TotalPagado:N2}.",
                    [
                        AuditChange.Of("Pagado", paidBefore, sale.VEN_TotalPagado),
                        AuditChange.Of("Estado", stateBefore, await StatusNameAsync(sale.VEN_EstadoVentaId, token))
                    ]);

                await _context.SaveChangesAsync(token);

                return OperationResult<SaleResponse>.Success((await GetByIdAsync(sale.VEN_Venta, true, token))!);
            },
            cancellationToken);
    }

    public Task<InvoiceResponse?> GetInvoiceAsync(int saleId, CancellationToken cancellationToken = default) =>
        _invoices.GetAsync(saleId, cancellationToken);

    public async Task<OperationResult<SaleResponse>> AnnulAsync(
        int saleId,
        AnnulSaleRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<SaleResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_VENTA? sale = await _context.Ventas
                        .Include(item => item.EstadoVenta)
                        .Include(item => item.Detalles)
                        .Include(item => item.Pagos).ThenInclude(payment => payment.MovimientoCaja!).ThenInclude(movement => movement.AperturaCaja)
                        .Include(item => item.Factura)
                        .FirstOrDefaultAsync(item => item.VEN_Venta == saleId, token);

                    if (sale is null)
                    {
                        return OperationResult<SaleResponse>.Missing("La venta no existe.");
                    }

                    if (sale.VEN_Anulada)
                    {
                        return OperationResult<SaleResponse>.Conflict($"La venta {sale.VEN_Numero} ya está anulada.");
                    }

                    // Si el cobro ya entró en un cierre de caja, anular descuadraría
                    // ese arqueo: corresponde una devolución.
                    if (sale.Pagos.Any(payment => !payment.VPA_Anulado && payment.MovimientoCaja is { AperturaCaja.ACA_Abierta: false }))
                    {
                        return OperationResult<SaleResponse>.Conflict(
                            $"La venta {sale.VEN_Numero} se cobró en una caja que ya se cerró; no puede anularse (corresponde una devolución).");
                    }

                    DateTime now = DateTime.UtcNow;
                    string reason = request.Reason.Trim();
                    string stateBefore = sale.EstadoVenta.EVE_Nombre;

                    // Inventario: vuelve a sus lotes y a la existencia.
                    PB_MOVIMIENTO_INVENTARIO? movement = await _context.MovimientosInventario
                        .Include(item => item.Detalles).ThenInclude(line => line.Lote)
                        .FirstOrDefaultAsync(item => item.TipoMovimiento.TMI_Codigo == SaleExitType &&
                                                     item.MOV_DocumentoReferencia == sale.VEN_Numero &&
                                                     !item.MOV_Anulado, token);

                    if (movement is not null)
                    {
                        foreach (PB_MOVIMIENTO_INVENTARIO_DETALLE line in movement.Detalles)
                        {
                            if (line.Lote is not null)
                            {
                                line.Lote.LOT_CantidadActual += line.MDE_Cantidad;
                                line.Lote.LOT_Activo = true;
                                line.Lote.LOT_FechaModificacion = now;
                            }
                        }

                        foreach (var group in movement.Detalles.GroupBy(line => line.MDE_LibroId))
                        {
                            PB_EXISTENCIA stock = await _context.Existencias.SingleAsync(
                                item => item.EXI_AlmacenId == sale.VEN_AlmacenId && item.EXI_LibroId == group.Key, token);

                            stock.EXI_CantidadDisponible += group.Sum(line => line.MDE_Cantidad);
                            stock.EXI_FechaUltimoMovimiento = now;
                            stock.EXI_FechaModificacion = now;
                        }

                        movement.MOV_Anulado = true;
                        movement.MOV_FechaAnulacion = now;
                        movement.MOV_MotivoAnulacion = reason;
                    }

                    // Pagos y caja.
                    foreach (PB_VENTA_PAGO payment in sale.Pagos.Where(payment => !payment.VPA_Anulado))
                    {
                        payment.VPA_Anulado = true;
                        payment.VPA_FechaAnulacion = now;
                        payment.VPA_MotivoAnulacion = reason;

                        if (payment.MovimientoCaja is { MCA_Anulado: false } cashMovement)
                        {
                            cashMovement.MCA_Anulado = true;
                            cashMovement.MCA_FechaAnulacion = now;
                            cashMovement.MCA_MotivoAnulacion = $"Anulación de la venta {sale.VEN_Numero}: {reason}";
                        }
                    }

                    // Si venía de un pedido, el pedido queda cancelado.
                    if (sale.VEN_PedidoClienteId is int orderId &&
                        await _orders.LoadAsync(orderId, token) is { } order)
                    {
                        order.PED_EstadoPedidoId = await _orders.StateIdAsync(OrderStatus.Cancelled, token);
                        order.PED_MotivoCancelacion = $"Venta {sale.VEN_Numero} anulada: {reason}";
                        order.PED_FechaModificacion = now;
                    }

                    if (sale.Factura is { FAC_Anulada: false } invoice)
                    {
                        InvoiceService.Annul(invoice, reason, now);
                    }

                    sale.VEN_Anulada = true;
                    sale.VEN_FechaAnulacion = now;
                    sale.VEN_MotivoAnulacion = reason;
                    sale.VEN_EstadoVentaId = await StatusIdAsync(SaleStatus.Annulled, token);
                    sale.VEN_FechaModificacion = now;

                    _audit.Add(
                        AuditActions.Annul,
                        SaleEntity,
                        sale.VEN_Venta.ToString(),
                        $"Anuló la venta {sale.VEN_Numero} por Q{sale.VEN_Total:N2}" +
                        (sale.Factura is null ? "" : $" y su factura {sale.Factura.FAC_Serie}-{sale.Factura.FAC_Numero}") +
                        $"; el inventario y los cobros se revirtieron: {reason}",
                        [AuditChange.Of("Estado", stateBefore, "Anulada")]);

                    await _context.SaveChangesAsync(token);

                    return OperationResult<SaleResponse>.Success((await GetByIdAsync(sale.VEN_Venta, true, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<SaleResponse>.Conflict(ConcurrencyMessage);
        }
    }

    // ───────────────────────────── Núcleo ─────────────────────────────

    /// <summary>
    /// Registra la venta con sus líneas, cobra y descuenta el inventario (sin
    /// confirmar la transacción). Valida existencia, caja y pagos.
    /// </summary>
    private async Task<OperationResult<PB_VENTA>> RegisterAsync(
        SaleDraft draft,
        IReadOnlyList<PaymentRequest> paymentRequests,
        string? notes,
        int? orderId,
        CurrentEmployee employee,
        CurrentEmployee? approvedBy,
        CancellationToken cancellationToken)
    {
        // Validación de inventario: sin existencia libre no hay venta.
        Dictionary<int, decimal> free = await _orders.FreeStockAsync(draft.WarehouseId, draft.Lines.Select(line => line.BookId), cancellationToken);
        string[] shortages = draft.Lines
            .Where(line => line.Quantity > free.GetValueOrDefault(line.BookId))
            .Select(line => $"Sin existencia suficiente de «{line.Title}»: se piden {line.Quantity:0} y hay {Math.Max(free.GetValueOrDefault(line.BookId), 0):0} disponible(s).")
            .ToArray();

        if (shortages.Length > 0)
        {
            return OperationResult<PB_VENTA>.Conflict(shortages);
        }

        decimal total = draft.Total;

        OperationResult<List<ResolvedPayment>> payments = await ResolvePaymentsAsync(
            paymentRequests, total, requireFull: !draft.IsCredit, cancellationToken);

        if (!payments.Succeeded)
        {
            return payments.As<PB_VENTA>();
        }

        PB_APERTURA_CAJA? session = await _cash.FindOpenSessionAsync(employee.Id, cancellationToken);

        if (session is null && (!draft.IsCredit || payments.Data!.Count > 0))
        {
            return OperationResult<PB_VENTA>.Conflict(CashService.NoOpenSessionMessage);
        }

        DateTime now = DateTime.UtcNow;

        PB_VENTA sale = new()
        {
            VEN_Numero = await _numbers.NextAsync(DocumentTypes.Sale, cancellationToken),
            VEN_PedidoClienteId = orderId,
            VEN_ClienteId = draft.CustomerId,
            VEN_SucursalId = draft.BranchId,
            VEN_AlmacenId = draft.WarehouseId,
            VEN_EmpleadoId = employee.Id,
            VEN_AperturaCajaId = session?.ACA_AperturaCaja,
            VEN_TipoVentaId = draft.SaleTypeId,
            VEN_EstadoVentaId = await StatusIdAsync(SaleStatus.Paid, cancellationToken),
            VEN_MonedaId = session?.ACA_MonedaId ?? await _context.Monedas.Where(item => item.MON_Codigo == "GTQ").Select(item => item.MON_Moneda).SingleAsync(cancellationToken),
            VEN_Fecha = now,
            VEN_TipoCambio = 1m,
            VEN_Subtotal = draft.Subtotal,
            VEN_Descuento = draft.Discount,
            VEN_Impuesto = draft.Tax,
            VEN_Total = total,
            VEN_FechaVencimientoCredito = draft.IsCredit ? BusinessTime.Today.AddDays(draft.CreditDays) : null,
            VEN_Observaciones = approvedBy is null ? notes : Append(notes, $"Autorizada por {approvedBy.Name} ({approvedBy.Code})."),
            VEN_InventarioProcesado = true,
            VEN_FechaProcesado = now,
            VEN_FechaCreacion = now
        };

        // Salida de inventario por venta (lotes PEPS).
        PB_MOVIMIENTO_INVENTARIO movement = new()
        {
            MOV_Numero = await _numbers.NextAsync(DocumentTypes.InventoryMovement, cancellationToken),
            MOV_TipoMovimientoId = await _context.TiposMovimientoInventario
                .Where(type => type.TMI_Codigo == SaleExitType)
                .Select(type => type.TMI_TipoMovimientoInventario)
                .SingleAsync(cancellationToken),
            MOV_AlmacenOrigenId = draft.WarehouseId,
            MOV_EmpleadoId = employee.Id,
            MOV_Fecha = now,
            MOV_DocumentoReferencia = sale.VEN_Numero,
            MOV_Procesado = true,
            MOV_FechaProcesado = now,
            MOV_FechaCreacion = now
        };

        int[] bookIds = draft.Lines.Select(line => line.BookId).ToArray();
        Dictionary<int, decimal> referenceCosts = await _context.Libros
            .Where(book => bookIds.Contains(book.LIB_Libro))
            .ToDictionaryAsync(book => book.LIB_Libro, book => book.LIB_CostoReferencia, cancellationToken);

        List<PB_EXISTENCIA> stocks = await _context.Existencias
            .Where(stock => stock.EXI_AlmacenId == draft.WarehouseId && bookIds.Contains(stock.EXI_LibroId))
            .ToListAsync(cancellationToken);

        int number = 1;

        foreach (PricedLine line in draft.Lines)
        {
            decimal unitCost = await _inventory.ConsumeLotsAsync(
                movement, draft.WarehouseId, line.BookId, line.Quantity, referenceCosts.GetValueOrDefault(line.BookId), cancellationToken);

            PB_EXISTENCIA stock = stocks.Single(item => item.EXI_LibroId == line.BookId);
            stock.EXI_CantidadDisponible -= line.Quantity;
            stock.EXI_FechaUltimoMovimiento = now;
            stock.EXI_FechaModificacion = now;

            sale.Detalles.Add(new PB_VENTA_DETALLE
            {
                VDE_LibroId = line.BookId,
                VDE_NumeroLinea = number++,
                VDE_Cantidad = line.Quantity,
                VDE_PrecioUnitario = line.UnitPrice,
                VDE_CostoUnitario = unitCost,
                VDE_PorcentajeDescuento = line.DiscountPercent,
                VDE_MontoDescuento = line.Amounts.Discount,
                VDE_PorcentajeImpuesto = line.TaxPercent,
                VDE_MontoImpuesto = line.Amounts.Tax,
                VDE_Subtotal = line.Amounts.Subtotal,
                VDE_Total = line.Amounts.Total
            });
        }

        _context.Ventas.Add(sale);
        _context.MovimientosInventario.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);

        if (session is not null && payments.Data!.Count > 0)
        {
            await AddPaymentsAsync(sale, session, payments.Data, employee.Id, cancellationToken);
        }

        // Factura electrónica (certificación simulada) en la misma transacción.
        await _invoices.IssueAsync(sale, cancellationToken);

        await UpdateStatusAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<PB_VENTA>.Success(sale);
    }

    private sealed record ResolvedPayment(
        PB_METODO_PAGO Method,
        decimal Amount,
        decimal Received,
        decimal Change,
        string? Reference,
        string? Authorization,
        string? LastFour);

    /// <summary>
    /// Valida los pagos: método activo, datos que exige cada método, cambio
    /// solo en efectivo y, al contado, que cubran exactamente el saldo.
    /// </summary>
    private async Task<OperationResult<List<ResolvedPayment>>> ResolvePaymentsAsync(
        IReadOnlyList<PaymentRequest> requests,
        decimal balance,
        bool requireFull,
        CancellationToken cancellationToken)
    {
        Dictionary<int, PB_METODO_PAGO> methods = await _context.MetodosPago
            .Where(method => method.MPA_Activo)
            .ToDictionaryAsync(method => method.MPA_MetodoPago, cancellationToken);

        List<string> errors = [];
        List<ResolvedPayment> resolved = [];

        for (int index = 0; index < requests.Count; index++)
        {
            PaymentRequest request = requests[index];
            string label = $"Pago {index + 1}";

            if (!methods.TryGetValue(request.PaymentMethodId!.Value, out PB_METODO_PAGO? method) || method.MPA_Codigo == PaymentMethodCode.Credit)
            {
                errors.Add($"{label}: el método de pago no existe o no puede usarse para cobrar.");
                continue;
            }

            decimal amount = request.Amount!.Value;
            decimal received = request.Received ?? amount;

            if (method.MPA_PermiteCambio)
            {
                if (received < amount)
                {
                    errors.Add($"{label}: lo recibido (Q{received:N2}) es menor que el monto del pago (Q{amount:N2}).");
                }
            }
            else if (request.Received is not null && request.Received != amount)
            {
                errors.Add($"{label}: {method.MPA_Nombre.ToLowerInvariant()} no admite cambio; lo recibido debe ser igual al monto.");
            }

            bool isCard = method.MPA_Codigo == PaymentMethodCode.Card;

            if (isCard && string.IsNullOrWhiteSpace(request.LastFourDigits))
            {
                errors.Add($"{label}: indique los últimos 4 dígitos de la tarjeta.");
            }
            else if (!isCard && method.MPA_RequiereReferencia && string.IsNullOrWhiteSpace(request.Reference))
            {
                errors.Add($"{label}: indique la referencia de la {method.MPA_Nombre.ToLowerInvariant()}.");
            }

            if (method.MPA_RequiereAutorizacion && string.IsNullOrWhiteSpace(request.Authorization))
            {
                errors.Add($"{label}: indique el número de autorización.");
            }

            resolved.Add(new ResolvedPayment(
                method,
                amount,
                method.MPA_PermiteCambio ? received : amount,
                method.MPA_PermiteCambio ? received - amount : 0m,
                Clean(request.Reference),
                Clean(request.Authorization),
                Clean(request.LastFourDigits)));
        }

        if (errors.Count > 0)
        {
            return OperationResult<List<ResolvedPayment>>.Failure(errors.ToArray());
        }

        decimal paid = resolved.Sum(payment => payment.Amount);

        if (paid > balance)
        {
            return OperationResult<List<ResolvedPayment>>.Failure(
                $"Los pagos (Q{paid:N2}) superan el saldo a cobrar (Q{balance:N2}). Para dar cambio indique lo recibido en efectivo.");
        }

        if (requireFull && paid < balance)
        {
            return OperationResult<List<ResolvedPayment>>.Failure(
                $"Una venta al contado debe pagarse completa: faltan Q{balance - paid:N2} de Q{balance:N2}.");
        }

        return OperationResult<List<ResolvedPayment>>.Success(resolved);
    }

    /// <summary>Registra los pagos con su movimiento de caja (tipo VENTA, por método).</summary>
    private async Task AddPaymentsAsync(
        PB_VENTA sale,
        PB_APERTURA_CAJA session,
        IReadOnlyList<ResolvedPayment> payments,
        int employeeId,
        CancellationToken cancellationToken)
    {
        PB_TIPO_MOVIMIENTO_CAJA saleType = await _context.TiposMovimientoCaja
            .SingleAsync(type => type.TMC_Codigo == CashMovementTypes.Sale, cancellationToken);

        DateTime now = DateTime.UtcNow;

        foreach (ResolvedPayment payment in payments)
        {
            PB_MOVIMIENTO_CAJA movement = await _cash.AddMovementAsync(
                session, saleType, payment.Method.MPA_MetodoPago, employeeId, payment.Amount,
                $"Cobro de la venta {sale.VEN_Numero}", payment.Reference, sale.VEN_Venta, cancellationToken);

            sale.Pagos.Add(new PB_VENTA_PAGO
            {
                VPA_MetodoPagoId = payment.Method.MPA_MetodoPago,
                VPA_MonedaId = session.ACA_MonedaId,
                MovimientoCaja = movement,
                VPA_Fecha = now,
                VPA_TipoCambio = 1m,
                VPA_MontoMoneda = payment.Amount,
                VPA_MontoBase = payment.Amount,
                VPA_MontoRecibido = payment.Received,
                VPA_CambioEntregado = payment.Change,
                VPA_Referencia = payment.Reference,
                VPA_Autorizacion = payment.Authorization,
                VPA_UltimosCuatroDigitos = payment.LastFour
            });

            sale.VEN_TotalPagado += payment.Amount;
            sale.VEN_CambioEntregado += payment.Change;
        }
    }

    /// <summary>Pagada si no hay saldo; al crédito, parcial o sin abonos.</summary>
    private async Task UpdateStatusAsync(PB_VENTA sale, CancellationToken cancellationToken)
    {
        string code = sale.VEN_TotalPagado >= sale.VEN_Total
            ? SaleStatus.Paid
            : sale.VEN_TotalPagado > 0 ? SaleStatus.Partial : SaleStatus.Credit;

        sale.VEN_EstadoVentaId = await StatusIdAsync(code, cancellationToken);
    }

    private static IEnumerable<AuditChange> SaleChanges(PB_VENTA sale) =>
    [
        AuditChange.Of("Total", null, sale.VEN_Total),
        AuditChange.Of("Descuento", null, sale.VEN_Descuento),
        AuditChange.Of("Pagado", null, sale.VEN_TotalPagado),
        AuditChange.Of("Unidades", null, sale.Detalles.Sum(line => line.VDE_Cantidad)),
        AuditChange.Of("Factura", null, sale.Factura is null ? null : $"{sale.Factura.FAC_Serie}-{sale.Factura.FAC_Numero}")
    ];

    private async Task<int> StatusIdAsync(string code, CancellationToken cancellationToken) =>
        await _context.EstadosVenta
            .Where(item => item.EVE_Codigo == code)
            .Select(item => item.EVE_EstadoVenta)
            .SingleAsync(cancellationToken);

    private async Task<string> StatusNameAsync(int id, CancellationToken cancellationToken) =>
        await _context.EstadosVenta
            .Where(item => item.EVE_EstadoVenta == id)
            .Select(item => item.EVE_Nombre)
            .SingleAsync(cancellationToken);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Append(string? notes, string text) =>
        string.IsNullOrWhiteSpace(notes) ? text : $"{notes} {text}";
}
