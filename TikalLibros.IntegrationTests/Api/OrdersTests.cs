using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Domain.Entities.Audit;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Pedidos: precios, descuentos, validación de inventario, reserva, aprobación y vencimiento.</summary>
public sealed class OrdersTests : SalesTestBase
{
    private static readonly string[] Approver = [OrderPermissions.View, OrderPermissions.Approve];

    public OrdersTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Crear_UsaElPrecioDeListaConIvaIncluidoYAplicaElDescuento()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 112m);

        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 2, 5m)));

        Assert.Matches(@"^PED-\d{6}$", order.Number);
        Assert.Equal(OrderStatus.Draft, order.StatusCode);
        OrderLineResponse line = Assert.Single(order.Lines);
        Assert.Equal(112m, line.UnitPrice);
        Assert.Equal(224m, line.Subtotal);
        Assert.Equal(11.20m, line.DiscountAmount);
        Assert.Equal(212.80m, line.Total);
        Assert.Equal(22.80m, line.TaxAmount); // 212.80 − 212.80 ÷ 1.12
        Assert.Equal((224m, 11.20m, 22.80m, 212.80m), (order.Subtotal, order.Discount, order.Tax, order.Total));
    }

    [Fact]
    public async Task Confirmar_ConExistencia_ReservaLaMercaderia()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 10);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 3, 0m)));

        OrderResponse confirmed = await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        Assert.Equal(OrderStatus.Reserved, confirmed.StatusCode);
        Assert.Equal(3m, Assert.Single(confirmed.Lines).Reserved);
        Assert.True(confirmed.CanInvoice);
        Assert.InRange(confirmed.ReservationExpiresAt!.Value, DateTime.UtcNow.AddDays(2.9), DateTime.UtcNow.AddDays(3.1));

        StockItemResponse stock = await StockAsync(admin, book);
        Assert.Equal((10m, 3m, 7m), (stock.Available, stock.Reserved, stock.Free));
    }

    [Fact]
    public async Task Confirmar_SinExistencia_Devuelve409YNoReserva()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int withStock = await CreateBookAsync(admin, stock: 2);
        int withoutStock = await CreateBookAsync(admin);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (withStock, 5, 0m), (withoutStock, 1, 0m)));

        HttpResponseMessage response = await ConfirmAsync(admin, order.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        string detail = (await ReadProblemDetailAsync(response))!;
        Assert.Contains("se piden 5 y hay 2", detail);
        Assert.Contains("se piden 1 y hay 0", detail);

        OrderResponse unchanged = (await admin.GetFromJsonAsync<OrderResponse>($"api/pedidos/{order.Id}"))!;
        Assert.Equal(OrderStatus.Draft, unchanged.StatusCode);
        Assert.Equal(0m, (await StockAsync(admin, withStock)).Reserved);
    }

    [Fact]
    public async Task Reserva_DeOtroPedido_NoSePuedeVolverAVender()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 5);

        OrderResponse first = await CreateOrderAsync(admin, Order(customer, (book, 4, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, first.Id));
        OrderResponse second = await CreateOrderAsync(admin, Order(customer, (book, 2, 0m)));

        HttpResponseMessage response = await ConfirmAsync(admin, second.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("se piden 2 y hay 1", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task DescuentoAlto_QuedaPorAprobarYOtroSupervisorLoAprueba()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient supervisor, _) = await EmployeeClientAsync(Approver);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 5);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 2, 15m)));

        OrderResponse pending = await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        Assert.Equal(OrderStatus.PendingApproval, pending.StatusCode);
        Assert.Contains(pending.ApprovalReasons, reason => reason.Contains("Descuento de 15 %"));
        Assert.Equal(0m, (await StockAsync(admin, book)).Reserved);
        Assert.False(pending.CanApprove); // quien lo registró no lo aprueba

        HttpResponseMessage own = await admin.PostAsJsonAsync($"api/pedidos/{order.Id}/aprobar", new ApproveOrderRequest());
        Assert.Equal(HttpStatusCode.Forbidden, own.StatusCode);

        OrderResponse approved = await OkAsync<OrderResponse>(await supervisor.PostAsJsonAsync(
            $"api/pedidos/{order.Id}/aprobar", new ApproveOrderRequest { Comment = "Cliente frecuente" }));

        Assert.Equal(OrderStatus.Reserved, approved.StatusCode);
        Assert.NotNull(approved.ApprovedBy);
        Assert.Equal("Cliente frecuente", approved.ApprovalComment);
        Assert.Equal(2m, (await StockAsync(admin, book)).Reserved);

        PB_AUDITORIA audit = await Factory.QueryAsync(context => context.Auditorias
            .Include(item => item.Detalles)
            .SingleAsync(item => item.AUD_Entidad == "PedidoCliente" && item.AUD_EntidadId == order.Id.ToString() && item.AUD_Accion == "APROBAR"));
        Assert.Contains(audit.Detalles, change => change.ADE_Campo == "Estado" && change.ADE_ValorNuevo == "Reservado");
        Assert.Contains(audit.Detalles, change => change.ADE_Campo == "Aprobado por");
    }

    [Fact]
    public async Task Rechazar_ExigeMotivoYAlCorregirVuelveABorrador()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient supervisor, _) = await EmployeeClientAsync(Approver);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 5);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 1, 20m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        Assert.Equal(HttpStatusCode.BadRequest, (await supervisor.PostAsJsonAsync(
            $"api/pedidos/{order.Id}/rechazar", new OrderReasonRequest { Reason = "no" })).StatusCode);

        OrderResponse rejected = await OkAsync<OrderResponse>(await supervisor.PostAsJsonAsync(
            $"api/pedidos/{order.Id}/rechazar", new OrderReasonRequest { Reason = "Descuento fuera de política" }));
        Assert.Equal(OrderStatus.Rejected, rejected.StatusCode);
        Assert.Equal("Descuento fuera de política", rejected.ApprovalComment);

        OrderResponse corrected = await OkAsync<OrderResponse>(
            await admin.PutAsJsonAsync($"api/pedidos/{order.Id}", Order(customer, (book, 1, 10m))));
        Assert.Equal(OrderStatus.Draft, corrected.StatusCode);
        Assert.Null(corrected.ApprovedBy);

        Assert.Equal(OrderStatus.Reserved, (await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id))).StatusCode);
    }

    [Fact]
    public async Task Credito_DentroDelLimiteReservaYExcedidoRequiereAprobacion()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin, creditLimit: 300m, creditDays: 30);
        int book = await CreateBookAsync(admin, price: 100m, stock: 10);

        SaveOrderRequest withinLimit = Order(customer, (book, 2, 0m));
        withinLimit.SaleType = SaleType.Credit;
        OrderResponse first = await CreateOrderAsync(admin, withinLimit);
        Assert.Equal(OrderStatus.Reserved, (await OkAsync<OrderResponse>(await ConfirmAsync(admin, first.Id))).StatusCode);

        // El pedido reservado ya usa Q200 del crédito: Q200 + Q200 excede Q300.
        OrderResponse second = await CreateOrderAsync(admin, withinLimit);
        OrderResponse pending = await OkAsync<OrderResponse>(await ConfirmAsync(admin, second.Id));

        Assert.Equal(OrderStatus.PendingApproval, pending.StatusCode);
        Assert.Contains(pending.ApprovalReasons, reason => reason.Contains("Excede el crédito"));
        Assert.Equal(200m, pending.CustomerCreditUsed);
    }

    [Fact]
    public async Task Credito_ClienteSinCreditoOUsuarioSinPermiso_SeRechaza()
    {
        HttpClient admin = await AdminAsync();
        int withoutCredit = await CreateCustomerAsync(admin);
        int withCredit = await CreateCustomerAsync(admin, creditLimit: 500m, creditDays: 15);
        int book = await CreateBookAsync(admin);

        SaveOrderRequest request = Order(withoutCredit, (book, 1, 0m));
        request.SaleType = SaleType.Credit;
        HttpResponseMessage noCredit = await admin.PostAsJsonAsync("api/pedidos", request);
        Assert.Equal(HttpStatusCode.Conflict, noCredit.StatusCode);
        Assert.Contains("no tiene crédito autorizado", await ReadProblemDetailAsync(noCredit));

        (HttpClient seller, _) = await EmployeeClientAsync(OrderPermissions.View, OrderPermissions.Create);
        request.CustomerId = withCredit;
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PostAsJsonAsync("api/pedidos", request)).StatusCode);
    }

    [Fact]
    public async Task Descuento_SinPermisoOMayorAlMaximo_SeRechaza()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin);
        (HttpClient seller, _) = await EmployeeClientAsync(OrderPermissions.View, OrderPermissions.Create);

        HttpResponseMessage noPermission = await seller.PostAsJsonAsync("api/pedidos", Order(customer, (book, 1, 5m)));
        Assert.Equal(HttpStatusCode.Forbidden, noPermission.StatusCode);
        Assert.Contains("aplicar descuentos", await ReadProblemDetailAsync(noPermission));

        HttpResponseMessage tooHigh = await admin.PostAsJsonAsync("api/pedidos", Order(customer, (book, 1, 60m)));
        Assert.Equal(HttpStatusCode.BadRequest, tooHigh.StatusCode);
        Assert.Contains("descuento máximo permitido es 50 %", await ReadProblemDetailAsync(tooHigh));

        Assert.Equal(HttpStatusCode.Created, (await seller.PostAsJsonAsync("api/pedidos", Order(customer, (book, 1, 0m)))).StatusCode);
    }

    [Fact]
    public async Task Datos_InvalidosOLibroSinPrecio_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin);

        HttpResponseMessage invalid = await admin.PostAsJsonAsync("api/pedidos", new SaveOrderRequest
        {
            CustomerId = customer,
            SaleType = "FIADO",
            Lines = [new OrderLineRequest { BookId = book, Quantity = 0, DiscountPercent = 1.555m }]
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var errors = (await ReadJsonAsync(invalid)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("SaleType", out _));
        Assert.True(errors.TryGetProperty("Lines[0].Quantity", out _));
        Assert.True(errors.TryGetProperty("Lines[0].DiscountPercent", out _));

        HttpResponseMessage repeated = await admin.PostAsJsonAsync("api/pedidos", Order(customer, (book, 1, 0m), (book, 2, 0m)));
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        Assert.True((await ReadJsonAsync(repeated)).GetProperty("errors").TryGetProperty("Lines", out _));

        int withoutPrice = await Factory.QueryAsync(async context =>
        {
            var prices = context.LibrosPrecios.Where(price => price.LIP_LibroId == book);
            await prices.ForEachAsync(price => price.LIP_Activo = false);
            await context.SaveChangesAsync();
            return book;
        });

        HttpResponseMessage noPrice = await admin.PostAsJsonAsync("api/pedidos", Order(customer, (withoutPrice, 1, 0m)));
        Assert.Equal(HttpStatusCode.BadRequest, noPrice.StatusCode);
        Assert.Contains("no tiene precio vigente", await ReadProblemDetailAsync(noPrice));
    }

    [Fact]
    public async Task Cancelar_PedidoReservado_LiberaLaExistenciaYNoSePuedeRepetir()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 4);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 4, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"api/pedidos/{order.Id}", Order(customer, (book, 1, 0m)))).StatusCode);

        OrderReasonRequest reason = new() { Reason = "El cliente ya no lo necesita" };
        OrderResponse cancelled = await OkAsync<OrderResponse>(await admin.PostAsJsonAsync($"api/pedidos/{order.Id}/cancelar", reason));

        Assert.Equal(OrderStatus.Cancelled, cancelled.StatusCode);
        Assert.Equal("El cliente ya no lo necesita", cancelled.CancellationReason);
        Assert.Equal((0m, 4m), ((await StockAsync(admin, book)).Reserved, (await StockAsync(admin, book)).Free));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"api/pedidos/{order.Id}/cancelar", reason)).StatusCode);
    }

    [Fact]
    public async Task ReservaVencida_PasaAVencidoYLiberaLaExistencia()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 3);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 3, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        await Factory.QueryAsync(async context =>
        {
            var stored = await context.PedidosCliente.SingleAsync(item => item.PED_PedidoCliente == order.Id);
            stored.PED_Fecha = DateTime.UtcNow.AddDays(-5);
            stored.PED_FechaVencimientoReserva = DateTime.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
            return true;
        });

        OrderResponse expired = (await admin.GetFromJsonAsync<OrderResponse>($"api/pedidos/{order.Id}"))!;

        Assert.Equal(OrderStatus.Expired, expired.StatusCode);
        Assert.Equal(0m, Assert.Single(expired.Lines).Reserved);
        Assert.Equal(3m, (await StockAsync(admin, book)).Free);
        Assert.False(expired.CanInvoice);
    }

    [Fact]
    public async Task Busqueda_CombinaEstadoClienteLibroYMontos()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m, stock: 10);
        int other = await CreateBookAsync(admin, price: 100m, stock: 10);

        OrderResponse big = await CreateOrderAsync(admin, Order(customer, (book, 5, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, big.Id));
        OrderResponse small = await CreateOrderAsync(admin, Order(customer, (book, 1, 0m)));
        await CreateOrderAsync(admin, Order(customer, (other, 5, 0m)));

        PagedResponse<OrderListItemResponse> reserved = (await admin.GetFromJsonAsync<PagedResponse<OrderListItemResponse>>(
            $"api/pedidos?customerId={customer}&status=RESERVADO&bookId={book}&minTotal=400"))!;
        Assert.Equal([big.Id], reserved.Items.Select(item => item.Id));

        PagedResponse<OrderListItemResponse> drafts = (await admin.GetFromJsonAsync<PagedResponse<OrderListItemResponse>>(
            $"api/pedidos?customerId={customer}&status=BORRADOR&bookId={book}&maxTotal=150"))!;
        Assert.Equal([small.Id], drafts.Items.Select(item => item.Id));

        string customerName = big.Customer;
        PagedResponse<OrderListItemResponse> byName = (await admin.GetFromJsonAsync<PagedResponse<OrderListItemResponse>>(
            $"api/pedidos?search={Uri.EscapeDataString(customerName)}&pageSize=50"))!;
        Assert.Equal(3, byName.Items.Count(item => item.CustomerId == customer));
    }

    [Fact]
    public async Task Permisos_SinPedidosVer403YSinAprobarNoAprueba()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient noOrders, _) = await EmployeeClientAsync(CustomerPermissions.View);
        (HttpClient seller, _) = await EmployeeClientAsync(OrderPermissions.View, OrderPermissions.Create);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 2);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 1, 30m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        Assert.Equal(HttpStatusCode.Forbidden, (await noOrders.GetAsync("api/pedidos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PostAsJsonAsync($"api/pedidos/{order.Id}/aprobar", new ApproveOrderRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PostAsJsonAsync(
            $"api/pedidos/{order.Id}/cancelar", new OrderReasonRequest { Reason = "Sin permiso para esto" })).StatusCode);
    }
}
