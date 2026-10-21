using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Domain.Entities.Sales;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Audit;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services.Sales;

/// <summary>
/// Pedidos de clientes: borrador → confirmar (valida existencia) → por
/// aprobar (descuento alto o crédito excedido) → reservado → facturado.
/// La reserva aparta la mercadería en PB_EXISTENCIA.EXI_CantidadReservada
/// durante los días configurados; al vencer o cancelar se libera.
/// </summary>
public sealed class OrderService : IOrderService
{
    private const string OrderEntity = "PedidoCliente";
    private const string LocalCurrency = "GTQ";
    private const string NitCode = "NIT";

    private const string ConcurrencyMessage =
        "La existencia cambió mientras se guardaba el pedido. Intente nuevamente.";

    private static readonly string[] EditableStates = [OrderStatus.Draft, OrderStatus.Rejected];

    private static readonly string[] CancellableStates =
        [OrderStatus.Draft, OrderStatus.PendingApproval, OrderStatus.Reserved, OrderStatus.Rejected];

    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;
    private readonly ISystemParameterService _parameters;
    private readonly AuditTrail _audit;

    public OrderService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees,
        ISystemParameterService parameters,
        AuditTrail audit)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
        _parameters = parameters;
        _audit = audit;
    }

    // ───────────────────────────── Consultas ─────────────────────────────

    public async Task<OrderCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default)
    {
        return new OrderCatalogsResponse
        {
            PriceLists = await _context.ListasPrecio
                .AsNoTracking()
                .Where(item => item.LPR_Activo)
                .OrderByDescending(item => item.LPR_EsPredeterminada)
                .ThenBy(item => item.LPR_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.LPR_ListaPrecio, Code = item.LPR_Codigo, Name = item.LPR_Nombre })
                .ToListAsync(cancellationToken),
            DefaultPriceListId = await DefaultPriceListIdAsync(cancellationToken),
            Warehouses = await _context.Almacenes
                .AsNoTracking()
                .Where(item => item.ALM_Activo)
                .OrderByDescending(item => item.ALM_EsPrincipal)
                .ThenBy(item => item.ALM_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.ALM_Almacen, Code = item.ALM_Codigo, Name = item.ALM_Nombre })
                .ToListAsync(cancellationToken),
            SaleTypes = await _context.TiposVenta
                .AsNoTracking()
                .Where(item => item.TVE_Activo)
                .OrderBy(item => item.TVE_TipoVenta)
                .Select(item => new CatalogOptionResponse { Id = item.TVE_TipoVenta, Code = item.TVE_Codigo, Name = item.TVE_Nombre })
                .ToListAsync(cancellationToken),
            Statuses = await _context.EstadosPedido
                .AsNoTracking()
                .Where(item => item.EPE_Activo && item.EPE_Codigo != "CONFIRMADO")
                .OrderBy(item => item.EPE_EstadoPedido)
                .Select(item => new CatalogOptionResponse { Id = item.EPE_EstadoPedido, Code = item.EPE_Codigo, Name = item.EPE_Nombre })
                .ToListAsync(cancellationToken),
            DiscountWithoutApproval = await _parameters.GetDecimalAsync(SystemParameterKeys.DiscountWithoutApproval, cancellationToken),
            MaximumDiscount = await _parameters.GetDecimalAsync(SystemParameterKeys.MaximumDiscount, cancellationToken),
            ReservationDays = (int)await _parameters.GetDecimalAsync(SystemParameterKeys.ReservationDays, cancellationToken)
        };
    }

    public async Task<IReadOnlyCollection<SalesBookOptionResponse>> SearchBooksAsync(
        string? search,
        int? priceListId,
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 2)
        {
            return [];
        }

        string value = search.Trim();
        int priceList = priceListId ?? await DefaultPriceListIdAsync(cancellationToken) ?? 0;
        int warehouse = warehouseId ?? await MainWarehouseIdAsync(cancellationToken) ?? 0;
        DateTime now = DateTime.UtcNow;

        return await _context.Libros
            .AsNoTracking()
            .Where(book => book.LIB_Activo && book.LIB_PermiteVenta &&
                           (book.LIB_Titulo.Contains(value) ||
                            book.LIB_Codigo.Contains(value) ||
                            (book.LIB_ISBN13 != null && book.LIB_ISBN13.Contains(value)) ||
                            (book.LIB_CodigoBarras != null && book.LIB_CodigoBarras.Contains(value))))
            .OrderBy(book => book.LIB_Titulo)
            .Take(20)
            .Select(book => new SalesBookOptionResponse
            {
                Id = book.LIB_Libro,
                Code = book.LIB_Codigo,
                Title = book.LIB_Titulo,
                Isbn13 = book.LIB_ISBN13,
                TaxPercent = book.Impuesto.IMP_Porcentaje,
                Price = book.Precios
                    .Where(price => price.LIP_ListaPrecioId == priceList && price.LIP_Activo &&
                                    price.LIP_FechaInicio <= now && (price.LIP_FechaFin == null || price.LIP_FechaFin > now))
                    .OrderByDescending(price => price.LIP_FechaInicio)
                    .Select(price => (decimal?)price.LIP_Precio)
                    .FirstOrDefault(),
                Available = _context.Existencias
                    .Where(stock => stock.EXI_LibroId == book.LIB_Libro && stock.EXI_AlmacenId == warehouse)
                    .Select(stock => stock.EXI_CantidadDisponible - stock.EXI_CantidadReservada)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<OrderListItemResponse>> GetPagedAsync(
        OrderQuery query,
        CancellationToken cancellationToken = default)
    {
        await ExpireReservationsAsync(cancellationToken);

        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_PEDIDO_CLIENTE> orders = _context.PedidosCliente.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            string status = query.Status.Trim().ToUpperInvariant();
            orders = orders.Where(item => item.EstadoPedido.EPE_Codigo == status);
        }

        if (!string.IsNullOrWhiteSpace(query.SaleType))
        {
            string saleType = query.SaleType.Trim().ToUpperInvariant();
            orders = saleType == SaleType.Cash
                ? orders.Where(item => item.TipoVenta == null || item.TipoVenta.TVE_Codigo == saleType)
                : orders.Where(item => item.TipoVenta != null && item.TipoVenta.TVE_Codigo == saleType);
        }

        if (query.CustomerId.HasValue)
        {
            orders = orders.Where(item => item.PED_ClienteId == query.CustomerId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            orders = orders.Where(item => item.PED_EmpleadoId == query.EmployeeId.Value);
        }

        if (query.BookId.HasValue)
        {
            orders = orders.Where(item => item.Detalles.Any(line => line.PDD_LibroId == query.BookId.Value));
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            orders = orders.Where(item => item.PED_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            orders = orders.Where(item => item.PED_Fecha < to);
        }

        if (query.MinTotal.HasValue)
        {
            orders = orders.Where(item => item.PED_Total >= query.MinTotal.Value);
        }

        if (query.MaxTotal.HasValue)
        {
            orders = orders.Where(item => item.PED_Total <= query.MaxTotal.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();

            orders = orders.Where(item =>
                item.PED_Numero.Contains(value) ||
                item.Cliente.CLI_Codigo.Contains(value) ||
                (item.Cliente.Persona.PersonaJuridica != null && item.Cliente.Persona.PersonaJuridica.PJU_RazonSocial.Contains(value)) ||
                (item.Cliente.Persona.PersonaNatural != null &&
                 (item.Cliente.Persona.PersonaNatural.PNA_PrimerNombre + " " + item.Cliente.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value)) ||
                item.Cliente.Persona.Identificaciones.Any(identification =>
                    identification.PID_Activo && identification.PID_Numero.Contains(value)));
        }

        int totalItems = await orders.CountAsync(cancellationToken);

        List<OrderListItemResponse> items = await orders
            .OrderByDescending(item => item.PED_Fecha)
            .ThenByDescending(item => item.PED_PedidoCliente)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new OrderListItemResponse
            {
                Id = item.PED_PedidoCliente,
                Number = item.PED_Numero,
                CustomerId = item.PED_ClienteId,
                CustomerCode = item.Cliente.CLI_Codigo,
                Customer = item.Cliente.Persona.PersonaJuridica != null
                    ? item.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                    : item.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                Date = item.PED_Fecha,
                StatusCode = item.EstadoPedido.EPE_Codigo,
                Status = item.EstadoPedido.EPE_Nombre,
                SaleType = item.TipoVenta != null ? item.TipoVenta.TVE_Nombre : "Contado",
                Units = item.Detalles.Sum(line => line.PDD_Cantidad),
                Discount = item.PED_Descuento,
                Total = item.PED_Total,
                ReservationExpiresAt = item.PED_FechaVencimientoReserva,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrderListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<OrderResponse?> GetByIdAsync(
        int id,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        await ExpireReservationsAsync(cancellationToken);

        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        return await BuildResponseAsync(id, employee?.Id, permissions, cancellationToken);
    }

    // ───────────────────────────── Registro ─────────────────────────────

    public async Task<OperationResult<OrderResponse>> CreateAsync(
        SaveOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<OrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                PB_PEDIDO_CLIENTE order = new()
                {
                    PED_EmpleadoId = employee.Id,
                    PED_Fecha = DateTime.UtcNow,
                    PED_MonedaId = await LocalCurrencyIdAsync(token),
                    PED_TipoCambio = 1m,
                    PED_EstadoPedidoId = await StateIdAsync(OrderStatus.Draft, token),
                    PED_Activo = true
                };

                OperationResult<OrderResponse>? invalid = await ApplyAsync(order, request, permissions, token);

                if (invalid is not null)
                {
                    return invalid;
                }

                order.PED_Numero = await _numbers.NextAsync(DocumentTypes.CustomerOrder, token);
                _context.PedidosCliente.Add(order);
                await _context.SaveChangesAsync(token);

                _audit.Add(
                    AuditActions.Create,
                    OrderEntity,
                    order.PED_PedidoCliente.ToString(),
                    $"Registró el pedido {order.PED_Numero} por Q{order.PED_Total:N2} ({order.Detalles.Count} libro(s)).",
                    [AuditChange.Of("Total", null, order.PED_Total), AuditChange.Of("Descuento", null, order.PED_Descuento)]);
                await _context.SaveChangesAsync(token);

                return OperationResult<OrderResponse>.Success(
                    (await BuildResponseAsync(order.PED_PedidoCliente, employee.Id, permissions, token))!);
            },
            cancellationToken);
    }

    public async Task<OperationResult<OrderResponse>> UpdateAsync(
        int id,
        SaveOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<OrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                PB_PEDIDO_CLIENTE? order = await LoadAsync(id, token);

                if (order is null)
                {
                    return OperationResult<OrderResponse>.Missing("El pedido no existe.");
                }

                if (!EditableStates.Contains(order.EstadoPedido.EPE_Codigo))
                {
                    return OperationResult<OrderResponse>.Conflict(
                        $"El pedido {order.PED_Numero} está {order.EstadoPedido.EPE_Nombre.ToLowerInvariant()} y ya no puede modificarse.");
                }

                decimal totalBefore = order.PED_Total;
                decimal discountBefore = order.PED_Descuento;
                bool wasRejected = order.EstadoPedido.EPE_Codigo == OrderStatus.Rejected;

                _context.PedidosClienteDetalles.RemoveRange(order.Detalles);
                order.Detalles.Clear();

                OperationResult<OrderResponse>? invalid = await ApplyAsync(order, request, permissions, token);

                if (invalid is not null)
                {
                    return invalid;
                }

                // Corregir un pedido rechazado lo devuelve a borrador para confirmarlo de nuevo.
                if (wasRejected)
                {
                    order.PED_EstadoPedidoId = await StateIdAsync(OrderStatus.Draft, token);
                    order.PED_EmpleadoAprobadorId = null;
                    order.PED_FechaAprobacion = null;
                    order.PED_ComentarioAprobacion = null;
                }

                order.PED_FechaModificacion = DateTime.UtcNow;

                _audit.Add(
                    AuditActions.Update,
                    OrderEntity,
                    order.PED_PedidoCliente.ToString(),
                    $"Modificó el pedido {order.PED_Numero}.",
                    [
                        AuditChange.Of("Total", totalBefore, order.PED_Total),
                        AuditChange.Of("Descuento", discountBefore, order.PED_Descuento),
                        AuditChange.Of("Estado", wasRejected ? "Rechazado" : "Borrador", "Borrador")
                    ]);

                await _context.SaveChangesAsync(token);

                return OperationResult<OrderResponse>.Success(
                    (await BuildResponseAsync(order.PED_PedidoCliente, employee.Id, permissions, token))!);
            },
            cancellationToken);
    }

    /// <summary>Línea con precio de lista, descuento e IVA ya calculados.</summary>
    internal sealed record PricedLine(
        int BookId,
        string Title,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountPercent,
        decimal TaxPercent,
        SaleLineAmounts Amounts);

    /// <summary>Pedido o venta validado y calculado, antes de guardarse.</summary>
    internal sealed record SaleDraft(
        int? CustomerId,
        int CreditDays,
        int SaleTypeId,
        string SaleTypeCode,
        int WarehouseId,
        int BranchId,
        IReadOnlyList<PricedLine> Lines)
    {
        public bool IsCredit => SaleTypeCode == SaleType.Credit;

        public decimal Subtotal => Lines.Sum(line => line.Amounts.Subtotal);

        public decimal Discount => Lines.Sum(line => line.Amounts.Discount);

        public decimal Tax => Lines.Sum(line => line.Amounts.Tax);

        public decimal Total => Lines.Sum(line => line.Amounts.Total);

        public decimal HighestDiscount => Lines.Select(line => line.DiscountPercent).DefaultIfEmpty(0).Max();
    }

    /// <summary>Cliente, condición de pago, precios y descuentos; deja el pedido calculado.</summary>
    private async Task<OperationResult<OrderResponse>?> ApplyAsync(
        PB_PEDIDO_CLIENTE order,
        SaveOrderRequest request,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken)
    {
        OperationResult<SaleDraft> prepared = await PrepareAsync(
            request.CustomerId, request.SaleType, request.PriceListId, request.WarehouseId, request.Lines, permissions, cancellationToken);

        if (!prepared.Succeeded)
        {
            return prepared.As<OrderResponse>();
        }

        SaleDraft draft = prepared.Data!;

        order.PED_ClienteId = draft.CustomerId!.Value;
        order.PED_TipoVentaId = draft.SaleTypeId;
        order.PED_AlmacenId = draft.WarehouseId;
        order.PED_SucursalId = draft.BranchId;
        order.PED_Observaciones = Clean(request.Notes);

        int number = 1;

        foreach (PricedLine line in draft.Lines)
        {
            order.Detalles.Add(new PB_PEDIDO_CLIENTE_DETALLE
            {
                PDD_LibroId = line.BookId,
                PDD_NumeroLinea = number++,
                PDD_Cantidad = line.Quantity,
                PDD_PrecioUnitario = line.UnitPrice,
                PDD_PorcentajeDescuento = line.DiscountPercent,
                PDD_MontoDescuento = line.Amounts.Discount,
                PDD_PorcentajeImpuesto = line.TaxPercent,
                PDD_MontoImpuesto = line.Amounts.Tax,
                PDD_Subtotal = line.Amounts.Subtotal,
                PDD_Total = line.Amounts.Total
            });
        }

        order.PED_Subtotal = draft.Subtotal;
        order.PED_Descuento = draft.Discount;
        order.PED_Impuesto = draft.Tax;
        order.PED_Total = draft.Total;

        return null;
    }

    /// <summary>
    /// Valida cliente, condición de pago, almacén, lista de precios, libros y
    /// descuentos, y calcula cada línea con el precio vigente. Lo usan los
    /// pedidos y la venta de mostrador (que admite consumidor final al contado).
    /// </summary>
    internal async Task<OperationResult<SaleDraft>> PrepareAsync(
        int? customerId,
        string saleTypeCode,
        int? priceListId,
        int? warehouseId,
        IReadOnlyList<OrderLineRequest> lines,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken)
    {
        var saleType = await _context.TiposVenta
            .Where(item => item.TVE_Codigo == saleTypeCode && item.TVE_Activo)
            .Select(item => new { item.TVE_TipoVenta, item.TVE_Codigo })
            .FirstOrDefaultAsync(cancellationToken);

        if (saleType is null)
        {
            return OperationResult<SaleDraft>.Failure("La condición de pago no existe o está inactiva.");
        }

        bool isCredit = saleType.TVE_Codigo == SaleType.Credit;
        int creditDays = 0;

        if (customerId is null)
        {
            if (isCredit)
            {
                return OperationResult<SaleDraft>.Failure("Seleccione el cliente: las ventas al crédito requieren un cliente.");
            }
        }
        else
        {
            var customer = await _context.Clientes
                .Where(item => item.CLI_Cliente == customerId)
                .Select(item => new { item.CLI_Codigo, item.CLI_Activo, item.CLI_LimiteCredito, item.CLI_DiasCredito })
                .FirstOrDefaultAsync(cancellationToken);

            if (customer is null)
            {
                return OperationResult<SaleDraft>.Failure("El cliente seleccionado no existe.");
            }

            if (!customer.CLI_Activo)
            {
                return OperationResult<SaleDraft>.Conflict($"El cliente {customer.CLI_Codigo} está inactivo; no se le pueden registrar pedidos ni ventas.");
            }

            if (isCredit && (customer.CLI_LimiteCredito <= 0 || customer.CLI_DiasCredito <= 0))
            {
                return OperationResult<SaleDraft>.Conflict(
                    $"El cliente {customer.CLI_Codigo} no tiene crédito autorizado (límite y días de crédito).");
            }

            creditDays = customer.CLI_DiasCredito;
        }

        if (isCredit && !permissions.CanSellOnCredit)
        {
            return OperationResult<SaleDraft>.Forbidden("No tiene permiso para vender al crédito.");
        }

        if (lines.Any(line => line.DiscountPercent > 0) && !permissions.CanApplyDiscount)
        {
            return OperationResult<SaleDraft>.Forbidden("No tiene permiso para aplicar descuentos.");
        }

        decimal maximumDiscount = await _parameters.GetDecimalAsync(SystemParameterKeys.MaximumDiscount, cancellationToken);

        int? warehouse = warehouseId ?? await MainWarehouseIdAsync(cancellationToken);
        var store = await _context.Almacenes
            .Where(item => item.ALM_Almacen == warehouse && item.ALM_Activo)
            .Select(item => new { item.ALM_Almacen, item.ALM_SucursalId })
            .FirstOrDefaultAsync(cancellationToken);

        if (store is null)
        {
            return OperationResult<SaleDraft>.Failure("El almacén seleccionado no existe o está inactivo.");
        }

        int? priceList = priceListId ?? await DefaultPriceListIdAsync(cancellationToken);

        if (priceList is null || !await _context.ListasPrecio.AnyAsync(item => item.LPR_ListaPrecio == priceList && item.LPR_Activo, cancellationToken))
        {
            return OperationResult<SaleDraft>.Failure("La lista de precios seleccionada no existe o está inactiva.");
        }

        int[] bookIds = lines.Select(line => line.BookId!.Value).ToArray();
        DateTime now = DateTime.UtcNow;

        var books = await _context.Libros
            .Where(book => bookIds.Contains(book.LIB_Libro))
            .Select(book => new
            {
                book.LIB_Libro,
                book.LIB_Titulo,
                book.LIB_Activo,
                book.LIB_PermiteVenta,
                Tax = book.Impuesto.IMP_Porcentaje,
                Price = book.Precios
                    .Where(price => price.LIP_ListaPrecioId == priceList && price.LIP_Activo &&
                                    price.LIP_FechaInicio <= now && (price.LIP_FechaFin == null || price.LIP_FechaFin > now))
                    .OrderByDescending(price => price.LIP_FechaInicio)
                    .Select(price => (decimal?)price.LIP_Precio)
                    .FirstOrDefault()
            })
            .ToDictionaryAsync(book => book.LIB_Libro, cancellationToken);

        List<string> errors = [];
        List<PricedLine> priced = [];

        for (int index = 0; index < lines.Count; index++)
        {
            OrderLineRequest line = lines[index];

            if (!books.TryGetValue(line.BookId!.Value, out var book))
            {
                errors.Add($"Línea {index + 1}: el libro no existe.");
                continue;
            }

            if (!book.LIB_Activo || !book.LIB_PermiteVenta)
            {
                errors.Add($"Línea {index + 1}: «{book.LIB_Titulo}» no está disponible para la venta.");
            }
            else if (book.Price is null)
            {
                errors.Add($"Línea {index + 1}: «{book.LIB_Titulo}» no tiene precio vigente en la lista seleccionada.");
            }

            if (line.DiscountPercent > maximumDiscount)
            {
                errors.Add($"Línea {index + 1}: el descuento máximo permitido es {maximumDiscount:0.##} %.");
            }

            if (errors.Count == 0)
            {
                priced.Add(new PricedLine(
                    book.LIB_Libro,
                    book.LIB_Titulo,
                    line.Quantity!.Value,
                    book.Price!.Value,
                    line.DiscountPercent,
                    book.Tax,
                    SaleLineAmounts.Calculate(line.Quantity!.Value, book.Price!.Value, line.DiscountPercent, book.Tax)));
            }
        }

        if (errors.Count > 0)
        {
            return OperationResult<SaleDraft>.Failure(errors.ToArray());
        }

        return OperationResult<SaleDraft>.Success(new SaleDraft(
            customerId, creditDays, saleType.TVE_TipoVenta, saleType.TVE_Codigo, store.ALM_Almacen, store.ALM_SucursalId, priced));
    }

    // ─────────────────────── Confirmación y aprobación ───────────────────────

    public async Task<OperationResult<OrderResponse>> ConfirmAsync(
        int id,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        await ExpireReservationsAsync(cancellationToken);

        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<OrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_PEDIDO_CLIENTE? order = await LoadAsync(id, token);

                    if (order is null)
                    {
                        return OperationResult<OrderResponse>.Missing("El pedido no existe.");
                    }

                    if (order.EstadoPedido.EPE_Codigo != OrderStatus.Draft)
                    {
                        return OperationResult<OrderResponse>.Conflict(
                            $"Solo se confirman pedidos en borrador; el pedido {order.PED_Numero} está {order.EstadoPedido.EPE_Nombre.ToLowerInvariant()}.");
                    }

                    // Validación de inventario: sin existencia libre no se confirma.
                    OperationResult<OrderResponse>? shortage = await CheckStockAsync(order, token);

                    if (shortage is not null)
                    {
                        return shortage;
                    }

                    IReadOnlyCollection<string> reasons = await ApprovalReasonsAsync(order, token);

                    if (reasons.Count > 0)
                    {
                        order.PED_EstadoPedidoId = await StateIdAsync(OrderStatus.PendingApproval, token);
                        order.PED_FechaModificacion = DateTime.UtcNow;

                        _audit.Add(
                            AuditActions.Confirm,
                            OrderEntity,
                            order.PED_PedidoCliente.ToString(),
                            $"Confirmó el pedido {order.PED_Numero}; requiere aprobación: {string.Join(" ", reasons)}",
                            [AuditChange.Of("Estado", "Borrador", "Por aprobar")]);
                    }
                    else
                    {
                        await ReserveAsync(order, token);

                        _audit.Add(
                            AuditActions.Confirm,
                            OrderEntity,
                            order.PED_PedidoCliente.ToString(),
                            $"Confirmó el pedido {order.PED_Numero} y reservó {order.Detalles.Sum(line => line.PDD_Cantidad):0} unidad(es).",
                            [AuditChange.Of("Estado", "Borrador", "Reservado")]);
                    }

                    await _context.SaveChangesAsync(token);

                    return OperationResult<OrderResponse>.Success(
                        (await BuildResponseAsync(order.PED_PedidoCliente, employee.Id, permissions, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<OrderResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<OrderResponse>> ApproveAsync(
        int id,
        ApproveOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        if (!permissions.CanApprove)
        {
            return OperationResult<OrderResponse>.Forbidden("No tiene permiso para aprobar pedidos.");
        }

        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<OrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_PEDIDO_CLIENTE? order = await LoadAsync(id, token);
                    OperationResult<OrderResponse>? invalid = CheckDecision(order, employee);

                    if (invalid is not null)
                    {
                        return invalid;
                    }

                    // La existencia pudo cambiar mientras esperaba la aprobación.
                    OperationResult<OrderResponse>? shortage = await CheckStockAsync(order!, token);

                    if (shortage is not null)
                    {
                        return shortage;
                    }

                    order!.PED_EmpleadoAprobadorId = employee.Id;
                    order.PED_FechaAprobacion = DateTime.UtcNow;
                    order.PED_ComentarioAprobacion = Clean(request.Comment);

                    await ReserveAsync(order, token);

                    _audit.Add(
                        AuditActions.Approve,
                        OrderEntity,
                        order.PED_PedidoCliente.ToString(),
                        $"Aprobó el pedido {order.PED_Numero} por Q{order.PED_Total:N2}" +
                        (order.PED_ComentarioAprobacion is null ? "." : $": {order.PED_ComentarioAprobacion}"),
                        [
                            AuditChange.Of("Estado", "Por aprobar", "Reservado"),
                            AuditChange.Of("Aprobado por", null, $"{employee.Name} ({employee.Code})")
                        ]);

                    await _context.SaveChangesAsync(token);

                    return OperationResult<OrderResponse>.Success(
                        (await BuildResponseAsync(order.PED_PedidoCliente, employee.Id, permissions, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<OrderResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<OperationResult<OrderResponse>> RejectAsync(
        int id,
        OrderReasonRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        if (!permissions.CanApprove)
        {
            return OperationResult<OrderResponse>.Forbidden("No tiene permiso para rechazar pedidos.");
        }

        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<OrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                PB_PEDIDO_CLIENTE? order = await LoadAsync(id, token);
                OperationResult<OrderResponse>? invalid = CheckDecision(order, employee);

                if (invalid is not null)
                {
                    return invalid;
                }

                order!.PED_EstadoPedidoId = await StateIdAsync(OrderStatus.Rejected, token);
                order.PED_EmpleadoAprobadorId = employee.Id;
                order.PED_FechaAprobacion = DateTime.UtcNow;
                order.PED_ComentarioAprobacion = Clean(request.Reason);
                order.PED_FechaModificacion = DateTime.UtcNow;

                _audit.Add(
                    AuditActions.Reject,
                    OrderEntity,
                    order.PED_PedidoCliente.ToString(),
                    $"Rechazó el pedido {order.PED_Numero}: {order.PED_ComentarioAprobacion}",
                    [
                        AuditChange.Of("Estado", "Por aprobar", "Rechazado"),
                        AuditChange.Of("Rechazado por", null, $"{employee.Name} ({employee.Code})")
                    ]);

                await _context.SaveChangesAsync(token);

                return OperationResult<OrderResponse>.Success(
                    (await BuildResponseAsync(order.PED_PedidoCliente, employee.Id, permissions, token))!);
            },
            cancellationToken);
    }

    public async Task<OperationResult<OrderResponse>> CancelAsync(
        int id,
        OrderReasonRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<OrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_PEDIDO_CLIENTE? order = await LoadAsync(id, token);

                    if (order is null)
                    {
                        return OperationResult<OrderResponse>.Missing("El pedido no existe.");
                    }

                    if (!CancellableStates.Contains(order.EstadoPedido.EPE_Codigo))
                    {
                        return OperationResult<OrderResponse>.Conflict(
                            $"El pedido {order.PED_Numero} está {order.EstadoPedido.EPE_Nombre.ToLowerInvariant()} y no puede cancelarse.");
                    }

                    string stateBefore = order.EstadoPedido.EPE_Nombre;
                    decimal released = await ReleaseAsync(order, token);

                    order.PED_EstadoPedidoId = await StateIdAsync(OrderStatus.Cancelled, token);
                    order.PED_MotivoCancelacion = Clean(request.Reason);
                    order.PED_FechaModificacion = DateTime.UtcNow;

                    _audit.Add(
                        AuditActions.Cancel,
                        OrderEntity,
                        order.PED_PedidoCliente.ToString(),
                        $"Canceló el pedido {order.PED_Numero}" +
                        (released > 0 ? $" y liberó {released:0} unidad(es) reservada(s)" : string.Empty) +
                        $": {order.PED_MotivoCancelacion}",
                        [AuditChange.Of("Estado", stateBefore, "Cancelado")]);

                    await _context.SaveChangesAsync(token);

                    return OperationResult<OrderResponse>.Success(
                        (await BuildResponseAsync(order.PED_PedidoCliente, employee.Id, permissions, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<OrderResponse>.Conflict(ConcurrencyMessage);
        }
    }

    public async Task<int> ExpireReservationsAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        List<int> expired = await _context.PedidosCliente
            .Where(item => item.EstadoPedido.EPE_Codigo == OrderStatus.Reserved &&
                           item.PED_FechaVencimientoReserva != null &&
                           item.PED_FechaVencimientoReserva < now)
            .Select(item => item.PED_PedidoCliente)
            .ToListAsync(cancellationToken);

        int count = 0;

        foreach (int id in expired)
        {
            try
            {
                OperationResult<bool> result = await _context.ExecuteInTransactionAsync(
                    async token =>
                    {
                        PB_PEDIDO_CLIENTE? order = await LoadAsync(id, token);

                        if (order is null || order.EstadoPedido.EPE_Codigo != OrderStatus.Reserved)
                        {
                            return OperationResult<bool>.Conflict("El pedido ya cambió de estado.");
                        }

                        decimal released = await ReleaseAsync(order, token);
                        order.PED_EstadoPedidoId = await StateIdAsync(OrderStatus.Expired, token);
                        order.PED_FechaModificacion = now;

                        _audit.Add(
                            AuditActions.Expire,
                            OrderEntity,
                            order.PED_PedidoCliente.ToString(),
                            $"La reserva del pedido {order.PED_Numero} venció sin facturarse; se liberaron {released:0} unidad(es).",
                            [AuditChange.Of("Estado", "Reservado", "Vencido")]);

                        await _context.SaveChangesAsync(token);
                        return OperationResult<bool>.Success(true);
                    },
                    cancellationToken);

                count += result.Succeeded ? 1 : 0;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Otra solicitud lo está procesando; se intentará en la siguiente consulta.
                _context.ChangeTracker.Clear();
            }
        }

        return count;
    }

    // ───────────────────────────── Reglas ─────────────────────────────

    /// <summary>El pedido espera aprobación y quien decide no es quien lo registró.</summary>
    private static OperationResult<OrderResponse>? CheckDecision(PB_PEDIDO_CLIENTE? order, CurrentEmployee employee)
    {
        if (order is null)
        {
            return OperationResult<OrderResponse>.Missing("El pedido no existe.");
        }

        if (order.EstadoPedido.EPE_Codigo != OrderStatus.PendingApproval)
        {
            return OperationResult<OrderResponse>.Conflict(
                $"Solo se aprueban o rechazan pedidos por aprobar; el pedido {order.PED_Numero} está {order.EstadoPedido.EPE_Nombre.ToLowerInvariant()}.");
        }

        return order.PED_EmpleadoId == employee.Id
            ? OperationResult<OrderResponse>.Forbidden("No puede aprobar ni rechazar un pedido que usted registró; debe hacerlo otro supervisor.")
            : null;
    }

    /// <summary>Cada línea necesita existencia libre (disponible − reservada) en el almacén.</summary>
    private async Task<OperationResult<OrderResponse>?> CheckStockAsync(PB_PEDIDO_CLIENTE order, CancellationToken cancellationToken)
    {
        Dictionary<int, decimal> free = await FreeStockAsync(order.PED_AlmacenId, order.Detalles.Select(line => line.PDD_LibroId), cancellationToken);
        List<string> errors = [];

        foreach (PB_PEDIDO_CLIENTE_DETALLE line in order.Detalles.OrderBy(line => line.PDD_NumeroLinea))
        {
            decimal available = free.GetValueOrDefault(line.PDD_LibroId);

            if (line.PDD_Cantidad > available)
            {
                errors.Add($"Sin existencia suficiente de «{line.Libro.LIB_Titulo}»: se piden {line.PDD_Cantidad:0} y hay {Math.Max(available, 0):0} disponible(s).");
            }
        }

        return errors.Count > 0 ? OperationResult<OrderResponse>.Conflict(errors.ToArray()) : null;
    }

    internal async Task<Dictionary<int, decimal>> FreeStockAsync(int warehouseId, IEnumerable<int> bookIds, CancellationToken cancellationToken)
    {
        int[] ids = bookIds.Distinct().ToArray();

        return await _context.Existencias
            .AsNoTracking()
            .Where(stock => stock.EXI_AlmacenId == warehouseId && ids.Contains(stock.EXI_LibroId))
            .ToDictionaryAsync(stock => stock.EXI_LibroId, stock => stock.EXI_CantidadDisponible - stock.EXI_CantidadReservada, cancellationToken);
    }

    /// <summary>Motivos por los que el pedido requiere aprobación (vacío si no la requiere).</summary>
    private async Task<IReadOnlyCollection<string>> ApprovalReasonsAsync(PB_PEDIDO_CLIENTE order, CancellationToken cancellationToken) =>
        await ApprovalReasonsAsync(
            order.Detalles.Select(line => line.PDD_PorcentajeDescuento).DefaultIfEmpty(0).Max(),
            await IsCreditAsync(order, cancellationToken),
            order.PED_ClienteId,
            order.PED_Total,
            order.PED_PedidoCliente,
            cancellationToken);

    /// <summary>
    /// Requiere aprobación si algún descuento supera el permitido sin
    /// aprobación, o si al crédito el monto excede el crédito disponible.
    /// </summary>
    internal async Task<IReadOnlyCollection<string>> ApprovalReasonsAsync(
        decimal highestDiscount,
        bool isCredit,
        int? customerId,
        decimal creditAmount,
        int? exceptOrderId,
        CancellationToken cancellationToken)
    {
        List<string> reasons = [];
        decimal threshold = await _parameters.GetDecimalAsync(SystemParameterKeys.DiscountWithoutApproval, cancellationToken);

        if (highestDiscount > threshold)
        {
            reasons.Add($"Descuento de {highestDiscount.ToString("0.##", CultureInfo.InvariantCulture)} % (sin aprobación se permite hasta {threshold.ToString("0.##", CultureInfo.InvariantCulture)} %).");
        }

        if (isCredit && customerId is int customer)
        {
            (decimal limit, decimal used) = await CreditAsync(customer, exceptOrderId, cancellationToken);

            if (used + creditAmount > limit)
            {
                reasons.Add($"Excede el crédito del cliente: límite Q{limit:N2}, saldo usado Q{used:N2} y se suman Q{creditAmount:N2}.");
            }
        }

        return reasons;
    }

    private async Task<bool> IsCreditAsync(PB_PEDIDO_CLIENTE order, CancellationToken cancellationToken) =>
        order.PED_TipoVentaId is int typeId &&
        await _context.TiposVenta.AnyAsync(item => item.TVE_TipoVenta == typeId && item.TVE_Codigo == SaleType.Credit, cancellationToken);

    /// <summary>
    /// Límite y crédito usado del cliente: saldo de sus ventas al crédito más
    /// los pedidos al crédito ya reservados (sin contar el pedido indicado).
    /// </summary>
    internal async Task<(decimal Limit, decimal Used)> CreditAsync(int customerId, int? exceptOrderId, CancellationToken cancellationToken)
    {
        decimal limit = await _context.Clientes
            .Where(item => item.CLI_Cliente == customerId)
            .Select(item => item.CLI_LimiteCredito)
            .FirstAsync(cancellationToken);

        decimal sales = await _context.Ventas
            .Where(sale => sale.VEN_ClienteId == customerId && !sale.VEN_Anulada && sale.TipoVenta.TVE_Codigo == SaleType.Credit)
            .SumAsync(sale => sale.VEN_Total - sale.VEN_TotalPagado, cancellationToken);

        decimal reserved = await _context.PedidosCliente
            .Where(item => item.PED_ClienteId == customerId &&
                           item.PED_PedidoCliente != exceptOrderId &&
                           item.EstadoPedido.EPE_Codigo == OrderStatus.Reserved &&
                           item.TipoVenta != null && item.TipoVenta.TVE_Codigo == SaleType.Credit)
            .SumAsync(item => item.PED_Total, cancellationToken);

        return (limit, sales + reserved);
    }

    /// <summary>Aparta la mercadería y pasa el pedido a reservado.</summary>
    private async Task ReserveAsync(PB_PEDIDO_CLIENTE order, CancellationToken cancellationToken)
    {
        int[] ids = order.Detalles.Select(line => line.PDD_LibroId).ToArray();
        List<PB_EXISTENCIA> stocks = await _context.Existencias
            .Where(stock => stock.EXI_AlmacenId == order.PED_AlmacenId && ids.Contains(stock.EXI_LibroId))
            .ToListAsync(cancellationToken);

        foreach (PB_PEDIDO_CLIENTE_DETALLE line in order.Detalles)
        {
            PB_EXISTENCIA stock = stocks.Single(item => item.EXI_LibroId == line.PDD_LibroId);
            stock.EXI_CantidadReservada += line.PDD_Cantidad;
            stock.EXI_FechaModificacion = DateTime.UtcNow;
            line.PDD_CantidadReservada = line.PDD_Cantidad;
        }

        decimal days = await _parameters.GetDecimalAsync(SystemParameterKeys.ReservationDays, cancellationToken);
        DateTime now = DateTime.UtcNow;

        order.PED_EstadoPedidoId = await StateIdAsync(OrderStatus.Reserved, cancellationToken);
        order.PED_FechaVencimientoReserva = (order.PED_Fecha > now ? order.PED_Fecha : now).AddDays((double)days);
        order.PED_FechaModificacion = now;
    }

    /// <summary>Devuelve a la existencia libre lo reservado y no facturado. Devuelve las unidades liberadas.</summary>
    internal async Task<decimal> ReleaseAsync(PB_PEDIDO_CLIENTE order, CancellationToken cancellationToken)
    {
        List<PB_PEDIDO_CLIENTE_DETALLE> reserved = order.Detalles.Where(line => line.PDD_CantidadReservada > 0).ToList();

        if (reserved.Count == 0)
        {
            return 0m;
        }

        int[] ids = reserved.Select(line => line.PDD_LibroId).ToArray();
        List<PB_EXISTENCIA> stocks = await _context.Existencias
            .Where(stock => stock.EXI_AlmacenId == order.PED_AlmacenId && ids.Contains(stock.EXI_LibroId))
            .ToListAsync(cancellationToken);

        decimal total = 0m;

        foreach (PB_PEDIDO_CLIENTE_DETALLE line in reserved)
        {
            PB_EXISTENCIA? stock = stocks.FirstOrDefault(item => item.EXI_LibroId == line.PDD_LibroId);

            if (stock is not null)
            {
                stock.EXI_CantidadReservada = Math.Max(0m, stock.EXI_CantidadReservada - line.PDD_CantidadReservada);
                stock.EXI_FechaModificacion = DateTime.UtcNow;
            }

            total += line.PDD_CantidadReservada;
            line.PDD_CantidadReservada = 0m;
        }

        return total;
    }

    internal Task<PB_PEDIDO_CLIENTE?> LoadAsync(int id, CancellationToken cancellationToken) =>
        _context.PedidosCliente
            .Include(item => item.EstadoPedido)
            .Include(item => item.TipoVenta)
            .Include(item => item.Detalles).ThenInclude(line => line.Libro)
            .FirstOrDefaultAsync(item => item.PED_PedidoCliente == id, cancellationToken);

    internal async Task<int> StateIdAsync(string code, CancellationToken cancellationToken) =>
        await _context.EstadosPedido
            .Where(item => item.EPE_Codigo == code)
            .Select(item => item.EPE_EstadoPedido)
            .SingleAsync(cancellationToken);

    // ───────────────────────────── Respuesta ─────────────────────────────

    internal async Task<OrderResponse?> BuildResponseAsync(
        int id,
        int? employeeId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken)
    {
        var row = await _context.PedidosCliente
            .AsNoTracking()
            .Where(item => item.PED_PedidoCliente == id)
            .Select(item => new
            {
                Response = new OrderResponse
                {
                    Id = item.PED_PedidoCliente,
                    Number = item.PED_Numero,
                    CustomerId = item.PED_ClienteId,
                    CustomerCode = item.Cliente.CLI_Codigo,
                    Customer = item.Cliente.Persona.PersonaJuridica != null
                        ? item.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                        : item.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + item.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                    CustomerNit = item.Cliente.Persona.Identificaciones
                        .Where(identification => identification.PID_Activo && identification.TipoIdentificacion.TID_Codigo == NitCode)
                        .Select(identification => identification.PID_Numero)
                        .FirstOrDefault(),
                    CustomerCreditLimit = item.Cliente.CLI_LimiteCredito,
                    WarehouseId = item.PED_AlmacenId,
                    Warehouse = item.Almacen.ALM_Nombre,
                    StatusCode = item.EstadoPedido.EPE_Codigo,
                    Status = item.EstadoPedido.EPE_Nombre,
                    SaleTypeCode = item.TipoVenta != null ? item.TipoVenta.TVE_Codigo : SaleType.Cash,
                    SaleType = item.TipoVenta != null ? item.TipoVenta.TVE_Nombre : "Contado",
                    Date = item.PED_Fecha,
                    ReservationExpiresAt = item.PED_FechaVencimientoReserva,
                    Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                               item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                               " (" + item.Empleado.EMP_Codigo + ")",
                    Notes = item.PED_Observaciones,
                    Subtotal = item.PED_Subtotal,
                    Discount = item.PED_Descuento,
                    Tax = item.PED_Impuesto,
                    Total = item.PED_Total,
                    ApprovedBy = item.EmpleadoAprobador != null
                        ? item.EmpleadoAprobador.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                          item.EmpleadoAprobador.Persona.PersonaNatural.PNA_PrimerApellido +
                          " (" + item.EmpleadoAprobador.EMP_Codigo + ")"
                        : null,
                    ApprovedAt = item.PED_FechaAprobacion,
                    ApprovalComment = item.PED_ComentarioAprobacion,
                    CancellationReason = item.PED_MotivoCancelacion,
                    Lines = item.Detalles
                        .OrderBy(line => line.PDD_NumeroLinea)
                        .Select(line => new OrderLineResponse
                        {
                            Id = line.PDD_PedidoClienteDetalle,
                            LineNumber = line.PDD_NumeroLinea,
                            BookId = line.PDD_LibroId,
                            BookCode = line.Libro.LIB_Codigo,
                            Title = line.Libro.LIB_Titulo,
                            Quantity = line.PDD_Cantidad,
                            Reserved = line.PDD_CantidadReservada,
                            Invoiced = line.PDD_CantidadFacturada,
                            UnitPrice = line.PDD_PrecioUnitario,
                            DiscountPercent = line.PDD_PorcentajeDescuento,
                            DiscountAmount = line.PDD_MontoDescuento,
                            TaxPercent = line.PDD_PorcentajeImpuesto,
                            TaxAmount = line.PDD_MontoImpuesto,
                            Subtotal = line.PDD_Subtotal,
                            Total = line.PDD_Total
                        })
                        .ToList()
                },
                item.PED_EmpleadoId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        OrderResponse response = row.Response;

        Dictionary<int, decimal> free = await FreeStockAsync(response.WarehouseId, response.Lines.Select(line => line.BookId), cancellationToken);

        foreach (OrderLineResponse line in response.Lines)
        {
            line.Available = free.GetValueOrDefault(line.BookId);
        }

        (_, response.CustomerCreditUsed) = await CreditAsync(response.CustomerId, response.Id, cancellationToken);

        if (response.StatusCode is OrderStatus.Draft or OrderStatus.PendingApproval or OrderStatus.Rejected)
        {
            PB_PEDIDO_CLIENTE preview = new()
            {
                PED_PedidoCliente = response.Id,
                PED_ClienteId = response.CustomerId,
                PED_Total = response.Total,
                PED_TipoVentaId = response.SaleTypeCode == SaleType.Credit
                    ? await _context.TiposVenta.Where(item => item.TVE_Codigo == SaleType.Credit).Select(item => (int?)item.TVE_TipoVenta).FirstAsync(cancellationToken)
                    : null,
                Detalles = response.Lines
                    .Select(line => new PB_PEDIDO_CLIENTE_DETALLE { PDD_PorcentajeDescuento = line.DiscountPercent })
                    .ToList()
            };

            response.ApprovalReasons = await ApprovalReasonsAsync(preview, cancellationToken);
        }

        string state = response.StatusCode;

        response.CanEdit = EditableStates.Contains(state);
        response.CanConfirm = state == OrderStatus.Draft;
        response.CanApprove = state == OrderStatus.PendingApproval && permissions.CanApprove && employeeId is not null && employeeId != row.PED_EmpleadoId;
        response.CanCancel = CancellableStates.Contains(state);
        response.CanInvoice = state == OrderStatus.Reserved;

        return response;
    }

    private async Task<int?> DefaultPriceListIdAsync(CancellationToken cancellationToken) =>
        await _context.ListasPrecio
            .Where(item => item.LPR_Activo)
            .OrderByDescending(item => item.LPR_EsPredeterminada)
            .ThenBy(item => item.LPR_ListaPrecio)
            .Select(item => (int?)item.LPR_ListaPrecio)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int?> MainWarehouseIdAsync(CancellationToken cancellationToken) =>
        await _context.Almacenes
            .Where(item => item.ALM_Activo)
            .OrderByDescending(item => item.ALM_EsPrincipal)
            .ThenBy(item => item.ALM_Almacen)
            .Select(item => (int?)item.ALM_Almacen)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int> LocalCurrencyIdAsync(CancellationToken cancellationToken) =>
        await _context.Monedas
            .Where(item => item.MON_Codigo == LocalCurrency)
            .Select(item => item.MON_Moneda)
            .SingleAsync(cancellationToken);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
