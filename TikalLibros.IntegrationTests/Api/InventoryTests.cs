using System.Net;
using System.Net.Http.Json;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

public sealed class InventoryTests : ApiTestBase
{
    public InventoryTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<int> CreateSupplierAsync(HttpClient admin)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("api/proveedores", new SaveSupplierRequest
        {
            PersonType = SaveSupplierRequest.LegalPerson,
            LegalName = $"Distribuidora {UniqueName("Izabal")}",
            Nit = RandomDigits(7) + "K"
        });

        return (await response.Content.ReadFromJsonAsync<SupplierResponse>())!.Id;
    }

    private static async Task<int> CreateBookAsync(HttpClient admin)
    {
        ProductCatalogsResponse catalogs = (await admin.GetFromJsonAsync<ProductCatalogsResponse>("api/productos/catalogos"))!;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/productos", new SaveProductRequest
        {
            Title = Unique("Libro de inventario "),
            Isbn13 = NewIsbn13(),
            LanguageId = catalogs.Languages.First().Id,
            FormatId = catalogs.Formats.First().Id,
            TaxId = catalogs.Taxes.First().Id,
            AllowsSale = true
        });

        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!.Id;
    }

    private static async Task<ReceptionResponse> PurchaseAsync(HttpClient admin, int supplier, int book, int quantity, decimal cost)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("api/recepciones/compra-directa", new DirectPurchaseRequest
        {
            Order = new SavePurchaseOrderRequest
            {
                SupplierId = supplier,
                Lines = [new PurchaseOrderLineRequest { BookId = book, Quantity = quantity, UnitCost = cost }]
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ReceptionResponse>())!;
    }

    private static async Task<InventoryCatalogsResponse> CatalogsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<InventoryCatalogsResponse>("api/inventario/catalogos"))!;

    private static async Task<StockItemResponse> StockAsync(HttpClient client, int book)
    {
        ProductResponse product = (await client.GetFromJsonAsync<ProductResponse>($"api/productos/{book}"))!;
        PagedResponse<StockItemResponse> page = (await client.GetFromJsonAsync<PagedResponse<StockItemResponse>>(
            $"api/inventario/existencias?search={product.Code}"))!;
        return Assert.Single(page.Items);
    }

    private static SaveAdjustmentRequest Adjustment(AdjustmentReasonResponse reason, int book, int quantity) => new()
    {
        ReasonId = reason.Id,
        Notes = "Conteo físico del inventario mensual.",
        Lines = [new AdjustmentLineRequest { BookId = book, Quantity = quantity }]
    };

    [Fact]
    public async Task Existencias_MuestranCantidadEstadoYCostoPromedio()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 4, 10m);
        await PurchaseAsync(admin, supplier, book, 6, 15m);

        StockItemResponse stock = await StockAsync(admin, book);

        Assert.Equal(10m, stock.Available);
        Assert.Equal(StockStatus.Normal, stock.Status);
        Assert.Equal(13m, stock.AverageCost); // (4·10 + 6·15) / 10
        Assert.Equal(130m, stock.TotalValue);
    }

    [Fact]
    public async Task Existencias_SinPermisoDeCostos_NoMuestranCostos()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 2, 10m);
        (string userName, _) = await CreateUserWithPermissionsAsync("Inventario.Ver", "Catalogo.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);

        StockItemResponse stock = await StockAsync(client, book);

        Assert.Null(stock.AverageCost);
        Assert.Null(stock.TotalValue);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"api/inventario/kardex?bookId={book}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/inventario/ajustes", new SaveAdjustmentRequest())).StatusCode);
    }

    [Fact]
    public async Task NivelMinimo_GeneraAlertaConCantidadSugerida()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 5, 10m);
        StockItemResponse stock = await StockAsync(admin, book);

        HttpResponseMessage levels = await admin.PutAsJsonAsync("api/inventario/existencias/niveles", new SaveStockLevelsRequest
        {
            WarehouseId = stock.WarehouseId,
            BookId = book,
            Minimum = 10,
            Maximum = 20,
            Location = "Estante a-3"
        });

        Assert.Equal(HttpStatusCode.OK, levels.StatusCode);
        StockItemResponse saved = (await levels.Content.ReadFromJsonAsync<StockItemResponse>())!;
        Assert.Equal(StockStatus.BelowMinimum, saved.Status);
        Assert.Equal("ESTANTE A-3", saved.Location);

        List<StockAlertResponse> alerts = (await admin.GetFromJsonAsync<List<StockAlertResponse>>("api/inventario/alertas"))!;
        StockAlertResponse alert = Assert.Single(alerts, item => item.BookId == book);
        Assert.Equal(StockStatus.BelowMinimum, alert.Level);
        Assert.Equal(15m, alert.SuggestedOrder);

        StockAlertSummaryResponse summary = (await admin.GetFromJsonAsync<StockAlertSummaryResponse>("api/inventario/alertas/resumen"))!;
        Assert.True(summary.BelowMinimum >= 1);
    }

    [Fact]
    public async Task NivelesInvalidos_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int book = await CreateBookAsync(admin);
        InventoryCatalogsResponse catalogs = await CatalogsAsync(admin);

        HttpResponseMessage response = await admin.PutAsJsonAsync("api/inventario/existencias/niveles", new SaveStockLevelsRequest
        {
            WarehouseId = catalogs.Warehouses.First().Id,
            BookId = book,
            Minimum = 10,
            Maximum = 5,
            Location = "Estante <3>"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Location", out _));
    }

    [Fact]
    public async Task AjusteNegativo_ConsumeLotesPepsYKardexMuestraSaldos()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 5, 10m);
        await PurchaseAsync(admin, supplier, book, 3, 12m);
        AdjustmentReasonResponse shortage = (await CatalogsAsync(admin)).AdjustmentReasons.First(reason => reason.Code == "FALTANTE");

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/inventario/ajustes", Adjustment(shortage, book, 6));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        AdjustmentResponse adjustment = (await response.Content.ReadFromJsonAsync<AdjustmentResponse>())!;
        Assert.Matches(@"^AJU-\d{6}$", adjustment.Number);
        AdjustmentLineResponse line = Assert.Single(adjustment.Lines);
        Assert.Equal(8m, line.SystemQuantity);
        Assert.Equal(2m, line.PhysicalQuantity);
        Assert.Equal(-6m, line.Difference);
        Assert.Equal(10.3333m, line.UnitCost); // 5 a Q10 y 1 a Q12

        KardexResponse kardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}"))!;
        Assert.Equal([5m, 8m, 3m, 2m], kardex.Entries.Select(entry => entry.Balance).ToArray());
        Assert.Equal(8m, kardex.TotalIn);
        Assert.Equal(6m, kardex.TotalOut);
        Assert.Equal(2m, kardex.ClosingBalance);

        Assert.Equal(2m, (await StockAsync(admin, book)).Available);
    }

    [Fact]
    public async Task Ajuste_MuestraNombreYCodigoDelEmpleadoYSeBuscaPorEllos()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 3, 10m);
        AdjustmentReasonResponse damage = (await CatalogsAsync(admin)).AdjustmentReasons.First(reason => reason.Code == "DANIO");

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/inventario/ajustes", Adjustment(damage, book, 1));
        AdjustmentResponse detail = (await response.Content.ReadFromJsonAsync<AdjustmentResponse>())!;

        PagedResponse<AdjustmentListItemResponse> all = (await admin.GetFromJsonAsync<PagedResponse<AdjustmentListItemResponse>>(
            "api/inventario/ajustes?pageSize=100"))!;
        AdjustmentListItemResponse item = Assert.Single(all.Items, adjustment => adjustment.Id == detail.Id);
        Assert.False(string.IsNullOrWhiteSpace(item.EmployeeCode));
        Assert.DoesNotContain(item.EmployeeCode, item.Employee);
        Assert.Equal($"{item.Employee} ({item.EmployeeCode})", detail.Employee);

        foreach (string search in new[] { item.Employee, item.EmployeeCode, detail.Number })
        {
            PagedResponse<AdjustmentListItemResponse> found = (await admin.GetFromJsonAsync<PagedResponse<AdjustmentListItemResponse>>(
                $"api/inventario/ajustes?pageSize=100&search={Uri.EscapeDataString(search)}"))!;
            Assert.Contains(found.Items, adjustment => adjustment.Id == detail.Id);
        }

        PagedResponse<AdjustmentListItemResponse> none = (await admin.GetFromJsonAsync<PagedResponse<AdjustmentListItemResponse>>(
            "api/inventario/ajustes?search=Nadie%20Inexistente"))!;
        Assert.Empty(none.Items);

        PagedResponse<MovementListItemResponse> movements = (await admin.GetFromJsonAsync<PagedResponse<MovementListItemResponse>>(
            $"api/inventario/movimientos?pageSize=100&search={Uri.EscapeDataString(item.Employee)}"))!;
        MovementListItemResponse movement = Assert.Single(movements.Items, entry => entry.Number == detail.MovementNumber);
        Assert.Equal(item.Employee, movement.Employee);
        Assert.Equal(item.EmployeeCode, movement.EmployeeCode);
    }

    [Fact]
    public async Task AjusteNegativo_MayorQueLaExistencia_Devuelve409SinCambios()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 2, 10m);
        AdjustmentReasonResponse damage = (await CatalogsAsync(admin)).AdjustmentReasons.First(reason => reason.Code == "DANIO");

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/inventario/ajustes", Adjustment(damage, book, 3));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("hay 2 disponibles", await ReadProblemDetailAsync(response));
        Assert.Equal(2m, (await StockAsync(admin, book)).Available);
    }

    [Fact]
    public async Task AjustePositivo_CreaLoteConUltimoCosto()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 1, 25m);
        AdjustmentReasonResponse surplus = (await CatalogsAsync(admin)).AdjustmentReasons.First(reason => reason.Code == "SOBRANTE");

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/inventario/ajustes", Adjustment(surplus, book, 4));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(25m, (await response.Content.ReadFromJsonAsync<AdjustmentResponse>())!.Lines.Single().UnitCost);
        Assert.Equal(5m, (await StockAsync(admin, book)).Available);
    }

    [Fact]
    public async Task AjusteAgotaExistencia_GeneraAlertaDeAgotado()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 2, 10m);
        AdjustmentReasonResponse damage = (await CatalogsAsync(admin)).AdjustmentReasons.First(reason => reason.Code == "DANIO");

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/inventario/ajustes", Adjustment(damage, book, 2))).StatusCode);

        List<StockAlertResponse> alerts = (await admin.GetFromJsonAsync<List<StockAlertResponse>>("api/inventario/alertas"))!;
        Assert.Equal(StockStatus.OutOfStock, Assert.Single(alerts, item => item.BookId == book).Level);
    }

    [Fact]
    public async Task Ajuste_SinObservacionCuandoElMotivoLaRequiere_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int book = await CreateBookAsync(admin);
        AdjustmentReasonResponse? reason = (await CatalogsAsync(admin)).AdjustmentReasons.FirstOrDefault(item => item.RequiresNotes);

        if (reason is null)
        {
            return;
        }

        SaveAdjustmentRequest request = Adjustment(reason, book, 1);
        request.Notes = null;

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/inventario/ajustes", request)).StatusCode);
    }

    [Fact]
    public async Task Kardex_ExcluyeRecepcionesAnuladas()
    {
        HttpClient admin = await AdminAsync();
        int supplier = await CreateSupplierAsync(admin);
        int book = await CreateBookAsync(admin);
        await PurchaseAsync(admin, supplier, book, 3, 10m);
        ReceptionResponse second = await PurchaseAsync(admin, supplier, book, 2, 10m);
        await admin.PostAsJsonAsync($"api/recepciones/{second.Id}/anular", new AnnulReceptionRequest { Reason = "Registro duplicado." });

        KardexResponse kardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}"))!;

        Assert.Single(kardex.Entries);
        Assert.Equal(3m, kardex.ClosingBalance);

        PagedResponse<MovementListItemResponse> movements = (await admin.GetFromJsonAsync<PagedResponse<MovementListItemResponse>>(
            $"api/inventario/movimientos?bookId={book}"))!;
        Assert.Equal(2, movements.TotalItems);
        Assert.Single(movements.Items, item => item.Annulled);
    }
}
