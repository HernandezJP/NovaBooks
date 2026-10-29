using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

public sealed class ReceptionsTests : ApiTestBase
{
    public ReceptionsTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<int> CreateSupplierAsync(HttpClient admin)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("api/proveedores", new SaveSupplierRequest
        {
            PersonType = SaveSupplierRequest.LegalPerson,
            LegalName = $"Distribuidora {UniqueName("Peten")}",
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
            Title = Unique("Libro recibido "),
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

    /// <summary>Crea y emite una orden.</summary>
    private static async Task<PurchaseOrderResponse> IssuedOrderAsync(HttpClient admin, SavePurchaseOrderRequest request)
    {
        HttpResponseMessage created = await admin.PostAsJsonAsync("api/compras", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        PurchaseOrderResponse order = (await created.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;

        HttpResponseMessage issued = await admin.PostAsync($"api/compras/{order.Id}/emitir", null);
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        return (await issued.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;
    }

    private static RegisterReceptionRequest Reception(PurchaseOrderResponse order, params (int Accepted, int Rejected, string? Reason)[] lines) => new()
    {
        PurchaseOrderId = order.Id,
        Lines = lines
            .Select((line, index) => new ReceptionLineRequest
            {
                PurchaseOrderLineId = order.Lines.ElementAt(index).Id,
                Accepted = line.Accepted,
                Rejected = line.Rejected,
                RejectReason = line.Reason
            })
            .ToList()
    };

    private Task<decimal> StockAsync(int bookId) =>
        Factory.QueryAsync(context => context.Existencias
            .Where(item => item.EXI_LibroId == bookId)
            .SumAsync(item => item.EXI_CantidadDisponible));

    [Fact]
    public async Task RecepcionParcial_CreaLoteMovimientoYSumaExistencias()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await IssuedOrderAsync(admin, Order(supplier, (book, 10, 50m, 10m)));

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones", Reception(order, (6, 1, "Portada dañada en el transporte")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ReceptionResponse reception = (await response.Content.ReadFromJsonAsync<ReceptionResponse>())!;
        Assert.Matches(@"^REC-\d{6}$", reception.Number);
        Assert.Matches(@"^MOV-\d{6}$", reception.MovementNumber);
        Assert.Equal("Recibida parcialmente", reception.PurchaseOrderStatus);

        ReceptionLineResponse line = Assert.Single(reception.Lines);
        Assert.Equal(45m, line.UnitCost); // 50 menos 10 % de descuento, sin IVA
        Assert.Equal(reception.Number, line.LotNumber);
        Assert.Equal(270m, reception.TotalCost);

        Assert.Equal(6m, await StockAsync(book));

        PurchaseOrderResponse updated = (await admin.GetFromJsonAsync<PurchaseOrderResponse>($"api/compras/{order.Id}"))!;
        Assert.Equal(PurchaseOrderStatus.Partial, updated.StatusCode);
        Assert.Equal(4m, updated.Lines.Single().Pending); // lo rechazado sigue pendiente
        Assert.False(updated.CanCancel);

        ProductResponse product = (await admin.GetFromJsonAsync<ProductResponse>($"api/productos/{book}"))!;
        Assert.Equal(45m, product.ReferenceCost);
    }

    [Fact]
    public async Task RecepcionCompleta_MarcaOrdenRecibidaYNoAdmiteMas()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await IssuedOrderAsync(admin, Order(supplier, (book, 5, 20m, 0m)));

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/recepciones", Reception(order, (3, 0, null)))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/recepciones", Reception(order, (2, 0, null)))).StatusCode);

        PurchaseOrderResponse received = (await admin.GetFromJsonAsync<PurchaseOrderResponse>($"api/compras/{order.Id}"))!;
        Assert.Equal(PurchaseOrderStatus.Received, received.StatusCode);
        Assert.Equal(5m, await StockAsync(book));

        HttpResponseMessage extra = await admin.PostAsJsonAsync("api/recepciones", Reception(order, (1, 0, null)));
        Assert.Equal(HttpStatusCode.Conflict, extra.StatusCode);
    }

    [Fact]
    public async Task Recepcion_MasDeLoPendiente_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await IssuedOrderAsync(admin, Order(supplier, (book, 5, 20m, 0m)));

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones", Reception(order, (5, 1, "Sobrante del proveedor")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("pendientes", await response.Content.ReadAsStringAsync());
        Assert.Equal(0m, await StockAsync(book));
    }

    [Fact]
    public async Task Recepcion_RechazoSinMotivoYFacturaConLetras_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await IssuedOrderAsync(admin, Order(supplier, (book, 5, 20m, 0m)));
        RegisterReceptionRequest request = Reception(order, (2, 1, null));
        request.SupplierDocumentSeries = "A#1";
        request.SupplierDocumentNumber = "12AB";

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("SupplierDocumentSeries", out _));
        Assert.True(errors.TryGetProperty("SupplierDocumentNumber", out _));
    }

    [Fact]
    public async Task Recepcion_OrdenEnBorrador_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        HttpResponseMessage created = await admin.PostAsJsonAsync("api/compras", Order(supplier, (book, 5, 20m, 0m)));
        PurchaseOrderResponse draft = (await created.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones", Reception(draft, (5, 0, null)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Recepcion_FacturaDelProveedorRepetida_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await IssuedOrderAsync(admin, Order(supplier, (book, 10, 20m, 0m)));

        RegisterReceptionRequest first = Reception(order, (2, 0, null));
        first.SupplierDocumentSeries = "a1b2";
        first.SupplierDocumentNumber = "123456";
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/recepciones", first)).StatusCode);

        RegisterReceptionRequest second = Reception(order, (2, 0, null));
        second.SupplierDocumentSeries = "A1B2";
        second.SupplierDocumentNumber = "123456";

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("A1B2-123456", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task CompraDirecta_CreaOrdenRecibidaYActualizaInventario()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones/compra-directa", new DirectPurchaseRequest
        {
            Order = Order(supplier, (book, 8, 30m, 0m)),
            SupplierDocumentNumber = "998877"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ReceptionResponse reception = (await response.Content.ReadFromJsonAsync<ReceptionResponse>())!;
        Assert.Equal("Recibida completamente", reception.PurchaseOrderStatus);
        Assert.Equal(8m, reception.Lines.Single().Accepted);
        Assert.Equal(8m, await StockAsync(book));
    }

    [Fact]
    public async Task CompraDirecta_SinPermisoDeAprobar_Devuelve403()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        (string userName, _) = await CreateUserWithPermissionsAsync("Compras.Crear", "Compras.Recibir");
        HttpClient client = await LoginAsync(userName, TestPassword);

        HttpResponseMessage response = await client.PostAsJsonAsync("api/recepciones/compra-directa", new DirectPurchaseRequest
        {
            Order = Order(supplier, (book, 1, 30m, 0m))
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompraEnDolares_ConvierteElCostoAMonedaLocal()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderCatalogsResponse catalogs = (await admin.GetFromJsonAsync<PurchaseOrderCatalogsResponse>("api/compras/catalogos"))!;
        SavePurchaseOrderRequest request = Order(supplier, (book, 2, 10m, 0m));
        request.CurrencyId = catalogs.Currencies.Single(currency => currency.Code == "USD").Id;
        request.ExchangeRate = 7.75m;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones/compra-directa", new DirectPurchaseRequest { Order = request });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(77.5m, (await response.Content.ReadFromJsonAsync<ReceptionResponse>())!.Lines.Single().UnitCost);
    }

    [Fact]
    public async Task Anular_RevierteExistenciasYEstadoDeLaOrden()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await IssuedOrderAsync(admin, Order(supplier, (book, 4, 20m, 0m)));
        ReceptionResponse reception = (await (await admin.PostAsJsonAsync("api/recepciones", Reception(order, (4, 0, null))))
            .Content.ReadFromJsonAsync<ReceptionResponse>())!;
        Assert.Equal(4m, await StockAsync(book));

        HttpResponseMessage annul = await admin.PostAsJsonAsync($"api/recepciones/{reception.Id}/anular", new AnnulReceptionRequest { Reason = "Se registró en la orden equivocada." });

        Assert.Equal(HttpStatusCode.OK, annul.StatusCode);
        Assert.Equal(ReceptionStatus.Annulled, (await annul.Content.ReadFromJsonAsync<ReceptionResponse>())!.StatusCode);
        Assert.Equal(0m, await StockAsync(book));
        Assert.Equal(PurchaseOrderStatus.Issued, (await admin.GetFromJsonAsync<PurchaseOrderResponse>($"api/compras/{order.Id}"))!.StatusCode);
        Assert.True(await Factory.QueryAsync(context => context.MovimientosInventario
            .AnyAsync(movement => movement.MOV_RecepcionCompraId == reception.Id && movement.MOV_Anulado)));

        HttpResponseMessage again = await admin.PostAsJsonAsync($"api/recepciones/{reception.Id}/anular", new AnnulReceptionRequest { Reason = "Segundo intento." });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Anular_ConSalidasDelLote_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        PurchaseOrderResponse order = await IssuedOrderAsync(admin, Order(supplier, (book, 4, 20m, 0m)));
        ReceptionResponse reception = (await (await admin.PostAsJsonAsync("api/recepciones", Reception(order, (4, 0, null))))
            .Content.ReadFromJsonAsync<ReceptionResponse>())!;

        // Simula una venta de una unidad del lote.
        await Factory.QueryAsync(async context =>
        {
            var lot = await context.Lotes.SingleAsync(item => item.LOT_LibroId == book);
            lot.LOT_CantidadActual = 3;
            return await context.SaveChangesAsync();
        });

        HttpResponseMessage annul = await admin.PostAsJsonAsync($"api/recepciones/{reception.Id}/anular", new AnnulReceptionRequest { Reason = "Error de registro." });

        Assert.Equal(HttpStatusCode.Conflict, annul.StatusCode);
        Assert.Equal(4m, await StockAsync(book));
    }
}
