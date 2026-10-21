using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Domain.Entities.Cash;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Ventas: con y sin inventario, desde pedido, pagos, crédito, descuentos
/// que requieren aprobación, anulación, auditoría y búsqueda.
/// </summary>
public sealed class SalesTests : SalesTestBase
{
    private static readonly string[] Cashier =
    [
        SalesPermissions.View, SalesPermissions.Create,
        CashPermissions.View, CashPermissions.Open, CashPermissions.Close,
        OrderPermissions.View, CustomerPermissions.View
    ];

    public SalesTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    /// <summary>Abre una caja nueva para el usuario si no tiene una abierta.</summary>
    private async Task<CashSessionResponse> EnsureCashAsync(HttpClient client)
    {
        HttpResponseMessage current = await client.GetAsync("api/caja/aperturas/actual");

        if (current.StatusCode == HttpStatusCode.OK)
        {
            return (await current.Content.ReadFromJsonAsync<CashSessionResponse>())!;
        }

        int register = await Factory.QueryAsync(async context =>
        {
            int branch = await context.Sucursales
                .Where(item => item.SUC_Codigo == OrganizationSeeder.MainBranchCode)
                .Select(item => item.SUC_Sucursal)
                .SingleAsync();

            PB_CAJA created = new()
            {
                CAJ_SucursalId = branch,
                CAJ_Codigo = Unique("CV").ToUpperInvariant(),
                CAJ_Nombre = Unique("Caja de ventas "),
                CAJ_Activo = true
            };

            context.Cajas.Add(created);
            await context.SaveChangesAsync();
            return created.CAJ_Caja;
        });

        return await OkAsync<CashSessionResponse>(
            await client.PostAsJsonAsync("api/caja/aperturas", new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = 100m }),
            HttpStatusCode.Created);
    }

    private static async Task<int> MethodAsync(HttpClient client, string code) =>
        (await client.GetFromJsonAsync<SaleCatalogsResponse>("api/ventas/catalogos"))!.PaymentMethods.Single(method => method.Code == code).Id;

    private static async Task<PaymentRequest> CashAsync(HttpClient client, decimal amount, decimal? received = null) =>
        new() { PaymentMethodId = await MethodAsync(client, PaymentMethodCode.Cash), Amount = amount, Received = received };

    private static async Task<PaymentRequest> CardAsync(HttpClient client, decimal amount) =>
        new() { PaymentMethodId = await MethodAsync(client, PaymentMethodCode.Card), Amount = amount, Authorization = "A12345", LastFourDigits = "4242" };

    private static DirectSaleRequest Sale(int? customer, IEnumerable<PaymentRequest> payments, params (int Book, int Quantity, decimal Discount)[] lines) => new()
    {
        CustomerId = customer,
        Lines = lines.Select(line => new OrderLineRequest { BookId = line.Book, Quantity = line.Quantity, DiscountPercent = line.Discount }).ToList(),
        Payments = payments.ToList()
    };

    private static Task<HttpResponseMessage> SellAsync(HttpClient client, DirectSaleRequest request) =>
        client.PostAsJsonAsync("api/ventas", request);

    [Fact]
    public async Task VentaConInventario_DescuentaExistenciaYCobraEnCajaConCambio()
    {
        HttpClient admin = await AdminAsync();
        CashSessionResponse cash = await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 112m, stock: 5);

        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 224m, received: 250m)], (book, 2, 0m))),
            HttpStatusCode.Created);

        Assert.Matches(@"^VEN-\d{6}$", sale.Number);
        Assert.Equal(SaleStatus.Paid, sale.StatusCode);
        Assert.Equal("Consumidor final", sale.Customer);
        Assert.Equal((224m, 224m, 0m, 26m), (sale.Total, sale.Paid, sale.Balance, sale.Change));
        Assert.Equal(50m, Assert.Single(sale.Lines).UnitCost); // costo de la compra (PEPS)
        Assert.NotNull(sale.InventoryMovementNumber);

        StockItemResponse stock = await StockAsync(admin, book);
        Assert.Equal(3m, stock.Available);

        CashSessionResponse after = (await admin.GetFromJsonAsync<CashSessionResponse>($"api/caja/aperturas/{cash.Id}"))!;
        Assert.Equal(cash.ExpectedCash + 224m, after.ExpectedCash);
        Assert.Contains(after.Movements, movement => movement.TypeCode == CashMovementTypes.Sale && movement.SaleNumber == sale.Number);

        KardexResponse kardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}"))!;
        Assert.Equal(2m, kardex.TotalOut);
        Assert.Equal(3m, kardex.ClosingBalance);
    }

    [Fact]
    public async Task VentaSinInventario_Devuelve409YNoCambiaNada()
    {
        HttpClient admin = await AdminAsync();
        CashSessionResponse cash = await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m, stock: 1);
        int salesBefore = await Factory.QueryAsync(context => context.Ventas.CountAsync());

        HttpResponseMessage response = await SellAsync(admin, Sale(null, [await CashAsync(admin, 300m)], (book, 3, 0m)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("se piden 3 y hay 1", await ReadProblemDetailAsync(response));
        Assert.Equal(1m, (await StockAsync(admin, book)).Available);
        Assert.Equal(salesBefore, await Factory.QueryAsync(context => context.Ventas.CountAsync()));
        Assert.Equal(cash.ExpectedCash, (await admin.GetFromJsonAsync<CashSessionResponse>($"api/caja/aperturas/{cash.Id}"))!.ExpectedCash);
    }

    [Fact]
    public async Task FacturarPedidoReservado_LiberaLaReservaYMarcaElPedidoFacturado()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 80m, stock: 6);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 3, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        SaleResponse sale = await OkAsync<SaleResponse>(await admin.PostAsJsonAsync(
            $"api/ventas/desde-pedido/{order.Id}",
            new InvoiceOrderRequest { Payments = [await CardAsync(admin, 240m)] }), HttpStatusCode.Created);

        Assert.Equal(order.Number, sale.OrderNumber);
        SalePaymentResponse payment = Assert.Single(sale.Payments);
        Assert.Equal(("TARJETA", "4242", 0m), (payment.PaymentMethodCode, payment.LastFourDigits, payment.Change));

        OrderResponse invoiced = (await admin.GetFromJsonAsync<OrderResponse>($"api/pedidos/{order.Id}"))!;
        Assert.Equal(OrderStatus.Invoiced, invoiced.StatusCode);
        Assert.Equal((3m, 0m), (Assert.Single(invoiced.Lines).Invoiced, invoiced.Lines.Single().Reserved));

        StockItemResponse stock = await StockAsync(admin, book);
        Assert.Equal((3m, 0m, 3m), (stock.Available, stock.Reserved, stock.Free));

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(
            $"api/ventas/desde-pedido/{order.Id}", new InvoiceOrderRequest { Payments = [await CardAsync(admin, 240m)] })).StatusCode);
    }

    [Theory]
    [InlineData("incompleto")]
    [InlineData("exceso")]
    [InlineData("tarjeta sin datos")]
    [InlineData("transferencia sin referencia")]
    [InlineData("efectivo insuficiente")]
    [InlineData("cambio con tarjeta")]
    public async Task Pagos_Invalidos_Devuelve400(string scenario)
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m, stock: 2);
        int transfer = await MethodAsync(admin, PaymentMethodCode.Transfer);

        PaymentRequest payment = scenario switch
        {
            "incompleto" => await CashAsync(admin, 90m),
            "exceso" => await CashAsync(admin, 110m),
            "tarjeta sin datos" => new PaymentRequest { PaymentMethodId = await MethodAsync(admin, PaymentMethodCode.Card), Amount = 100m },
            "transferencia sin referencia" => new PaymentRequest { PaymentMethodId = transfer, Amount = 100m },
            "efectivo insuficiente" => await CashAsync(admin, 100m, received: 80m),
            _ => new PaymentRequest
            {
                PaymentMethodId = await MethodAsync(admin, PaymentMethodCode.Card),
                Amount = 100m,
                Received = 120m,
                Authorization = "A1",
                LastFourDigits = "1111"
            }
        };

        HttpResponseMessage response = await SellAsync(admin, Sale(null, [payment], (book, 1, 0m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2m, (await StockAsync(admin, book)).Available);
    }

    [Fact]
    public async Task Venta_SinCajaAbierta_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        int book = await CreateBookAsync(admin, price: 50m, stock: 1);

        HttpResponseMessage response = await SellAsync(cashier, Sale(null, [await CashAsync(cashier, 50m)], (book, 1, 0m)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("No tiene una caja abierta", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Credito_ConAbonoInicialYAbonosHastaPagar()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin, creditLimit: 1000m, creditDays: 30);
        int book = await CreateBookAsync(admin, price: 150m, stock: 4);

        DirectSaleRequest request = Sale(customer, [await CashAsync(admin, 100m)], (book, 2, 0m));
        request.SaleType = SaleType.Credit;
        SaleResponse sale = await OkAsync<SaleResponse>(await SellAsync(admin, request), HttpStatusCode.Created);

        Assert.Equal(SaleStatus.Partial, sale.StatusCode);
        Assert.Equal((300m, 100m, 200m), (sale.Total, sale.Paid, sale.Balance));
        Assert.Equal(BusinessTime.Today.AddDays(30), sale.CreditDueDate);

        OrderResponse creditCheck = await CreateOrderAsync(admin, Order(customer, (book, 1, 0m)));
        Assert.Equal(200m, creditCheck.CustomerCreditUsed); // el saldo de la venta usa crédito

        SaleResponse paid = await OkAsync<SaleResponse>(await admin.PostAsJsonAsync(
            $"api/ventas/{sale.Id}/pagos", new AddPaymentsRequest { Payments = [await CashAsync(admin, 200m)] }));

        Assert.Equal(SaleStatus.Paid, paid.StatusCode);
        Assert.Equal(0m, paid.Balance);
        Assert.Equal(2, paid.Payments.Count);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(
            $"api/ventas/{sale.Id}/pagos", new AddPaymentsRequest { Payments = [await CashAsync(admin, 1m)] })).StatusCode);
    }

    [Fact]
    public async Task Credito_ExcedeElLimite_SoloLoVendeQuienPuedeAprobar()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync([.. Cashier, SalesPermissions.SellOnCredit]);
        await EnsureCashAsync(cashier);
        int customer = await CreateCustomerAsync(admin, creditLimit: 100m, creditDays: 15);
        int book = await CreateBookAsync(admin, price: 150m, stock: 2);

        DirectSaleRequest request = Sale(customer, [], (book, 1, 0m));
        request.SaleType = SaleType.Credit;

        HttpResponseMessage denied = await SellAsync(cashier, request);
        Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        string detail = (await ReadProblemDetailAsync(denied))!;
        Assert.Contains("Excede el crédito", detail);
        Assert.Contains("Registre un pedido", detail);

        await EnsureCashAsync(admin);
        SaleResponse approved = await OkAsync<SaleResponse>(await SellAsync(admin, request), HttpStatusCode.Created);
        Assert.Equal(SaleStatus.Credit, approved.StatusCode);
        Assert.Contains("Autorizada por", approved.Notes);
    }

    [Fact]
    public async Task DescuentoAlto_EnMostrador_RequiereAprobacionYConPermisoSeAplica()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync([.. Cashier, SalesPermissions.ApplyDiscount]);
        await EnsureCashAsync(cashier);
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 200m, stock: 3);

        // 10 % está permitido sin aprobación.
        SaleResponse allowed = await OkAsync<SaleResponse>(
            await SellAsync(cashier, Sale(null, [await CashAsync(cashier, 180m)], (book, 1, 10m))), HttpStatusCode.Created);
        Assert.Equal(20m, allowed.Discount);

        HttpResponseMessage denied = await SellAsync(cashier, Sale(null, [await CashAsync(cashier, 150m)], (book, 1, 25m)));
        Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        Assert.Contains("Descuento de 25 %", await ReadProblemDetailAsync(denied));

        SaleResponse supervised = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 150m)], (book, 1, 25m))), HttpStatusCode.Created);
        Assert.Equal((50m, 150m), (supervised.Discount, supervised.Total));
    }

    [Fact]
    public async Task Anular_DevuelveInventarioYCobrosYCancelaElPedido()
    {
        HttpClient admin = await AdminAsync();
        CashSessionResponse cash = await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 60m, stock: 5);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 2, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));
        decimal expectedBefore = (await admin.GetFromJsonAsync<CashSessionResponse>($"api/caja/aperturas/{cash.Id}"))!.ExpectedCash;

        SaleResponse sale = await OkAsync<SaleResponse>(await admin.PostAsJsonAsync(
            $"api/ventas/desde-pedido/{order.Id}", new InvoiceOrderRequest { Payments = [await CashAsync(admin, 120m)] }), HttpStatusCode.Created);
        Assert.Equal(3m, (await StockAsync(admin, book)).Available);

        AnnulSaleRequest reason = new() { Reason = "Error al registrar el cliente" };
        SaleResponse annulled = await OkAsync<SaleResponse>(await admin.PostAsJsonAsync($"api/ventas/{sale.Id}/anular", reason));

        Assert.True(annulled.Annulled);
        Assert.Equal(SaleStatus.Annulled, annulled.StatusCode);
        Assert.All(annulled.Payments, payment => Assert.True(payment.Annulled));
        Assert.Equal(5m, (await StockAsync(admin, book)).Available);
        Assert.Equal(expectedBefore, (await admin.GetFromJsonAsync<CashSessionResponse>($"api/caja/aperturas/{cash.Id}"))!.ExpectedCash);
        Assert.Equal(OrderStatus.Cancelled, (await admin.GetFromJsonAsync<OrderResponse>($"api/pedidos/{order.Id}"))!.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"api/ventas/{sale.Id}/anular", reason)).StatusCode);

        var audit = await Factory.QueryAsync(context => context.Auditorias
            .Where(item => item.AUD_Entidad == "Venta" && item.AUD_EntidadId == sale.Id.ToString())
            .OrderBy(item => item.AUD_Auditoria)
            .Select(item => item.AUD_Accion)
            .ToListAsync());
        Assert.Equal(["VENDER", "ANULAR"], audit);
    }

    [Fact]
    public async Task Anular_VentaDeCajaCerrada_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        CashSessionResponse cash = await EnsureCashAsync(cashier);
        int book = await CreateBookAsync(admin, price: 40m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(cashier, Sale(null, [await CashAsync(cashier, 40m)], (book, 1, 0m))), HttpStatusCode.Created);
        await OkAsync<CashSessionResponse>(await cashier.PostAsJsonAsync(
            $"api/caja/aperturas/{cash.Id}/cerrar", new CloseCashSessionRequest { CountedCash = cash.ExpectedCash + 40m }));

        HttpResponseMessage response = await admin.PostAsJsonAsync($"api/ventas/{sale.Id}/anular", new AnnulSaleRequest { Reason = "Cliente devolvió el libro" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("caja que ya se cerró", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Permisos_SinCrearVenderSinAnularYSinVerCostos()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient viewer, _) = await EmployeeClientAsync(SalesPermissions.View);
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        await EnsureCashAsync(cashier);
        int book = await CreateBookAsync(admin, price: 30m, stock: 2);

        Assert.Equal(HttpStatusCode.Forbidden, (await SellAsync(viewer, Sale(null, [await CashAsync(admin, 30m)], (book, 1, 0m)))).StatusCode);

        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(cashier, Sale(null, [await CashAsync(cashier, 30m)], (book, 1, 0m))), HttpStatusCode.Created);

        Assert.Null(Assert.Single((await cashier.GetFromJsonAsync<SaleResponse>($"api/ventas/{sale.Id}"))!.Lines).UnitCost);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.PostAsJsonAsync(
            $"api/ventas/{sale.Id}/anular", new AnnulSaleRequest { Reason = "Intento sin permiso" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SellAsync(cashier, Sale(null, [await CashAsync(cashier, 27m)], (book, 1, 10m)))).StatusCode);
    }

    [Fact]
    public async Task Busqueda_CombinaClienteSaldoMetodoYLibro()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin, creditLimit: 5000m, creditDays: 30);
        int book = await CreateBookAsync(admin, price: 100m, stock: 10);

        DirectSaleRequest credit = Sale(customer, [], (book, 1, 0m));
        credit.SaleType = SaleType.Credit;
        SaleResponse pending = await OkAsync<SaleResponse>(await SellAsync(admin, credit), HttpStatusCode.Created);
        SaleResponse card = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(customer, [await CardAsync(admin, 200m)], (book, 2, 0m))), HttpStatusCode.Created);

        PagedResponse<SaleListItemResponse> withBalance = (await admin.GetFromJsonAsync<PagedResponse<SaleListItemResponse>>(
            $"api/ventas?customerId={customer}&withBalance=true"))!;
        Assert.Equal([pending.Id], withBalance.Items.Select(item => item.Id));

        int cardMethod = await MethodAsync(admin, PaymentMethodCode.Card);
        PagedResponse<SaleListItemResponse> byCard = (await admin.GetFromJsonAsync<PagedResponse<SaleListItemResponse>>(
            $"api/ventas?customerId={customer}&paymentMethodId={cardMethod}&bookId={book}&minTotal=150"))!;
        Assert.Equal([card.Id], byCard.Items.Select(item => item.Id));

        PagedResponse<SaleListItemResponse> byNumber = (await admin.GetFromJsonAsync<PagedResponse<SaleListItemResponse>>(
            $"api/ventas?search={card.Number}"))!;
        Assert.Equal(card.Id, Assert.Single(byNumber.Items).Id);
    }
}
