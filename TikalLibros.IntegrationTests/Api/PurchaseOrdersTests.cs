using System.Net;
using System.Net.Http.Json;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

public sealed class PurchaseOrdersTests : ApiTestBase
{
    public PurchaseOrdersTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<int> CreateSupplierAsync(HttpClient admin)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("api/proveedores", new SaveSupplierRequest
        {
            PersonType = SaveSupplierRequest.LegalPerson,
            LegalName = $"Distribuidora {UniqueName("Quetzal")}",
            Nit = RandomDigits(7) + "K"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SupplierResponse>())!.Id;
    }

    private static async Task<int> CreateBookAsync(HttpClient admin)
    {
        ProductCatalogsResponse catalogs = (await admin.GetFromJsonAsync<ProductCatalogsResponse>("api/productos/catalogos"))!;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/productos", new SaveProductRequest
        {
            Title = Unique("Libro para comprar "),
            Isbn13 = NewIsbn13(),
            LanguageId = catalogs.Languages.First().Id,
            FormatId = catalogs.Formats.First().Id,
            TaxId = catalogs.Taxes.First().Id,
            AllowsSale = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!.Id;
    }

    private static SavePurchaseOrderRequest Order(int supplierId, params (int BookId, int Quantity, decimal Cost, decimal Discount)[] lines) => new()
    {
        SupplierId = supplierId,
        ExpectedDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7),
        Lines = lines
            .Select(line => new PurchaseOrderLineRequest
            {
                BookId = line.BookId,
                Quantity = line.Quantity,
                UnitCost = line.Cost,
                DiscountPercent = line.Discount
            })
            .ToList()
    };

    private static async Task<PurchaseOrderResponse> CreateAsync(HttpClient client, SavePurchaseOrderRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("api/compras", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;
    }

    [Fact]
    public async Task Crear_GeneraNumeroBorradorYCalculaTotales()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);

        PurchaseOrderResponse order = await CreateAsync(admin, Order(supplier, (book, 10, 50m, 10m)));

        Assert.Matches(@"^OC-\d{6}$", order.Number);
        Assert.Equal(PurchaseOrderStatus.Draft, order.StatusCode);
        Assert.True(order.CanEdit && order.CanIssue && order.CanCancel && !order.CanReceive);
        Assert.Matches(@"^.+ \(.+\)$", order.RequestedBy); // Nombre Apellido (código)

        PurchaseOrderLineResponse line = Assert.Single(order.Lines);
        decimal expectedTax = decimal.Round(450m * line.TaxPercent / 100m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(500m, line.Subtotal);
        Assert.Equal(50m, line.DiscountAmount);
        Assert.Equal(expectedTax, line.TaxAmount);
        Assert.Equal(450m + expectedTax, order.Total);
        Assert.Equal(10m, line.Pending);
    }

    [Fact]
    public async Task Crear_NumerosConsecutivosYUnicos()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);

        PurchaseOrderResponse first = await CreateAsync(admin, Order(supplier, (book, 1, 10m, 0m)));
        PurchaseOrderResponse second = await CreateAsync(admin, Order(supplier, (book, 1, 10m, 0m)));

        Assert.Equal(int.Parse(first.Number[3..]) + 1, int.Parse(second.Number[3..]));
    }

    [Fact]
    public async Task Crear_LineasInvalidas_DevuelveErroresPorLinea()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);

        SavePurchaseOrderRequest request = Order(supplier, (book, 0, 10.555m, 120m));

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/compras", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Lines[0].Quantity", out _));
        Assert.True(errors.TryGetProperty("Lines[0].UnitCost", out _));
        Assert.True(errors.TryGetProperty("Lines[0].DiscountPercent", out _));
    }

    [Fact]
    public async Task Crear_LibroRepetidoOSinLineas_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);

        HttpResponseMessage duplicated = await admin.PostAsJsonAsync("api/compras", Order(supplier, (book, 1, 10m, 0m), (book, 2, 10m, 0m)));
        HttpResponseMessage empty = await admin.PostAsJsonAsync("api/compras", Order(supplier));

        Assert.Equal(HttpStatusCode.BadRequest, duplicated.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task Crear_ProveedorInactivo_SeRechaza()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await admin.PatchAsJsonAsync($"api/proveedores/{supplier}/estado", new ChangeSupplierStatusRequest { IsActive = false });

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/compras", Order(supplier, (book, 1, 10m, 0m)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("inactivo", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Actualizar_SincronizaLineasYRecalcula()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int first = await CreateBookAsync(admin);
        int second = await CreateBookAsync(admin);
        int third = await CreateBookAsync(admin);

        PurchaseOrderResponse order = await CreateAsync(admin, Order(supplier, (first, 2, 10m, 0m), (second, 3, 20m, 0m)));

        HttpResponseMessage response = await admin.PutAsJsonAsync($"api/compras/{order.Id}", Order(supplier, (second, 5, 20m, 0m), (third, 1, 100m, 0m)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PurchaseOrderResponse updated = (await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;
        Assert.Equal([second, third], updated.Lines.Select(line => line.BookId).Order().ToArray());
        Assert.Equal(200m, updated.Subtotal);
        Assert.Equal(order.Number, updated.Number);
    }

    [Fact]
    public async Task Emitir_BloqueaEdicionYPermiteCancelarConMotivo()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await CreateAsync(admin, Order(supplier, (book, 4, 25m, 0m)));

        HttpResponseMessage issue = await admin.PostAsync($"api/compras/{order.Id}/emitir", null);
        Assert.Equal(HttpStatusCode.OK, issue.StatusCode);
        PurchaseOrderResponse issued = (await issue.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;
        Assert.Equal(PurchaseOrderStatus.Issued, issued.StatusCode);
        Assert.True(issued.CanReceive && !issued.CanEdit);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"api/compras/{order.Id}", Order(supplier, (book, 1, 1m, 0m)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"api/compras/{order.Id}/emitir", null)).StatusCode);

        // Con una orden emitida el proveedor no puede desactivarse.
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PatchAsJsonAsync($"api/proveedores/{supplier}/estado", new ChangeSupplierStatusRequest { IsActive = false })).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"api/compras/{order.Id}/cancelar", new CancelPurchaseOrderRequest { Reason = "" })).StatusCode);

        HttpResponseMessage cancel = await admin.PostAsJsonAsync($"api/compras/{order.Id}/cancelar", new CancelPurchaseOrderRequest { Reason = "El proveedor no tiene existencias." });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        PurchaseOrderResponse cancelled = (await cancel.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;
        Assert.Equal(PurchaseOrderStatus.Cancelled, cancelled.StatusCode);
        Assert.Contains("El proveedor no tiene existencias.", cancelled.Notes);
    }

    [Fact]
    public async Task UsuarioSinEmpleado_NoPuedeCrearOrdenes()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        (string userName, _) = await CreateUserWithPermissionsAsync("Compras.Ver", "Compras.Crear");
        HttpClient client = await LoginAsync(userName, TestPassword);

        HttpResponseMessage response = await client.PostAsJsonAsync("api/compras", Order(supplier, (book, 1, 10m, 0m)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("empleado", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task SinPermisoDeAprobar_NoPuedeEmitir()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await CreateAsync(admin, Order(supplier, (book, 1, 10m, 0m)));
        (string userName, _) = await CreateUserWithPermissionsAsync("Compras.Ver", "Compras.Crear");
        HttpClient client = await LoginAsync(userName, TestPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"api/compras/{order.Id}/emitir", null)).StatusCode);
    }

    [Fact]
    public async Task BuscarLibros_DevuelveImpuestoYUltimoCosto()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await CreateAsync(admin, Order(supplier, (book, 1, 37.5m, 0m)));
        ProductResponse product = (await admin.GetFromJsonAsync<ProductResponse>($"api/productos/{book}"))!;

        List<PurchaseBookOptionResponse>? options =
            await admin.GetFromJsonAsync<List<PurchaseBookOptionResponse>>($"api/compras/libros?search={Uri.EscapeDataString(product.Title)}");

        PurchaseBookOptionResponse option = Assert.Single(options!);
        Assert.Equal(37.5m, option.LastCost);
    }
}
