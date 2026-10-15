using System.Net;
using System.Net.Http.Json;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Employees;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Application.DTOs.Users;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Flujo completo del sprint 2 con un usuario de los roles Compras e
/// Inventario: proveedor → orden → detalle → recepciones → entrada de
/// inventario → nivel mínimo y alertas → ajuste → kardex.
/// </summary>
public sealed class PurchasingFlowTests : ApiTestBase
{
    public PurchasingFlowTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static string NewDpi()
    {
        while (true)
        {
            string body = RandomDigits(8);
            int total = body.Select((digit, index) => (digit - '0') * (index + 2)).Sum();

            if (total % 11 < 10)
            {
                return $"{body}{total % 11}0101";
            }
        }
    }

    /// <summary>Empleado con usuario de los roles Compras e Inventario.</summary>
    private async Task<HttpClient> BuyerAsync(HttpClient admin)
    {
        EmployeeCatalogsResponse catalogs = (await admin.GetFromJsonAsync<EmployeeCatalogsResponse>("api/empleados/catalogos"))!;
        HttpResponseMessage employee = await admin.PostAsJsonAsync("api/empleados", new SaveEmployeeRequest
        {
            FirstName = "Carla",
            LastName = UniqueName("Compras"),
            Dpi = NewDpi(),
            PositionId = catalogs.Positions.Single(position => position.Name == "Encargado de compras").Id,
            BranchId = catalogs.Branches.First().Id,
            HireDate = DateOnly.FromDateTime(DateTime.Today)
        });
        Assert.Equal(HttpStatusCode.Created, employee.StatusCode);

        string userName = Unique("compras");
        HttpResponseMessage user = await admin.PostAsJsonAsync("api/usuarios", new CreateUserRequest
        {
            UserName = userName,
            Email = $"{userName}@tikal.test",
            Password = TestPassword,
            Roles = ["Compras", "Inventario"],
            EmployeeId = (await employee.Content.ReadFromJsonAsync<EmployeeResponse>())!.Id
        });
        Assert.Equal(HttpStatusCode.Created, user.StatusCode);

        return await LoginAsync(userName, TestPassword);
    }

    private static async Task<T> OkAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task FlujoCompleto_DeProveedorAKardex()
    {
        HttpClient admin = await AdminAsync();
        HttpClient buyer = await BuyerAsync(admin);

        // 1. Proveedor (lo registra el usuario de compras).
        SupplierResponse supplier = await OkAsync<SupplierResponse>(
            await buyer.PostAsJsonAsync("api/proveedores", new SaveSupplierRequest
            {
                PersonType = SaveSupplierRequest.LegalPerson,
                LegalName = $"Distribuidora {UniqueName("Flujo")}, S.A.",
                Nit = RandomDigits(7) + "K",
                CreditLimit = 10000m,
                CreditDays = 30
            }),
            HttpStatusCode.Created);

        // 2. Libro (lo registra el administrador del catálogo).
        ProductCatalogsResponse productCatalogs = (await admin.GetFromJsonAsync<ProductCatalogsResponse>("api/productos/catalogos"))!;
        ProductResponse book = await OkAsync<ProductResponse>(
            await admin.PostAsJsonAsync("api/productos", new SaveProductRequest
            {
                Title = Unique("Libro del flujo "),
                Isbn13 = NewIsbn13(),
                LanguageId = productCatalogs.Languages.First().Id,
                FormatId = productCatalogs.Formats.First().Id,
                TaxId = productCatalogs.Taxes.First().Id,
                AllowsSale = true
            }),
            HttpStatusCode.Created);

        // 3. Nivel mínimo y máximo antes de tener existencia: alerta de agotado.
        InventoryCatalogsResponse inventoryCatalogs = (await buyer.GetFromJsonAsync<InventoryCatalogsResponse>("api/inventario/catalogos"))!;
        int warehouse = inventoryCatalogs.Warehouses.First().Id;
        StockItemResponse levels = await OkAsync<StockItemResponse>(
            await buyer.PutAsJsonAsync("api/inventario/existencias/niveles", new SaveStockLevelsRequest
            {
                WarehouseId = warehouse,
                BookId = book.Id,
                Minimum = 10,
                Maximum = 30
            }));
        Assert.Equal(StockStatus.OutOfStock, levels.Status);

        // 4. Orden de compra con su detalle; se emite.
        PurchaseOrderResponse order = await OkAsync<PurchaseOrderResponse>(
            await buyer.PostAsJsonAsync("api/compras", new SavePurchaseOrderRequest
            {
                SupplierId = supplier.Id,
                ExpectedDate = DateOnly.FromDateTime(DateTime.Today).AddDays(5),
                Lines = [new PurchaseOrderLineRequest { BookId = book.Id, Quantity = 20, UnitCost = 40m, DiscountPercent = 5m }]
            }),
            HttpStatusCode.Created);
        Assert.Equal(PurchaseOrderStatus.Draft, order.StatusCode);
        Assert.Equal(760m, order.Subtotal - order.Discount); // 20 × 40 − 5 %

        order = await OkAsync<PurchaseOrderResponse>(await buyer.PostAsync($"api/compras/{order.Id}/emitir", null));
        Assert.Equal(PurchaseOrderStatus.Issued, order.StatusCode);

        // 5. Recepción parcial: 8 aceptadas y 2 rechazadas.
        ReceptionResponse first = await OkAsync<ReceptionResponse>(
            await buyer.PostAsJsonAsync("api/recepciones", new RegisterReceptionRequest
            {
                PurchaseOrderId = order.Id,
                SupplierDocumentSeries = "FAC1",
                SupplierDocumentNumber = RandomDigits(6),
                Lines =
                [
                    new ReceptionLineRequest
                    {
                        PurchaseOrderLineId = order.Lines.Single().Id,
                        Accepted = 8,
                        Rejected = 2,
                        RejectReason = "Ejemplares con la portada dañada"
                    }
                ]
            }),
            HttpStatusCode.Created);
        Assert.Equal(38m, first.Lines.Single().UnitCost); // costo neto sin IVA

        PurchaseOrderResponse partial = (await buyer.GetFromJsonAsync<PurchaseOrderResponse>($"api/compras/{order.Id}"))!;
        Assert.Equal(PurchaseOrderStatus.Partial, partial.StatusCode);
        Assert.Equal(12m, partial.Lines.Single().Pending);

        // La existencia (8) queda bajo el mínimo (10): alerta con cantidad sugerida.
        StockAlertResponse lowAlert = Assert.Single(
            (await buyer.GetFromJsonAsync<List<StockAlertResponse>>("api/inventario/alertas"))!,
            alert => alert.BookId == book.Id);
        Assert.Equal(StockStatus.BelowMinimum, lowAlert.Level);
        Assert.Equal(22m, lowAlert.SuggestedOrder);

        // Con la orden abierta, el proveedor no puede desactivarse.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await buyer.PatchAsJsonAsync($"api/proveedores/{supplier.Id}/estado", new ChangeSupplierStatusRequest { IsActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PatchAsJsonAsync($"api/proveedores/{supplier.Id}/estado", new ChangeSupplierStatusRequest { IsActive = false })).StatusCode);

        // 6. Segunda recepción: el resto. La orden queda recibida.
        await OkAsync<ReceptionResponse>(
            await buyer.PostAsJsonAsync("api/recepciones", new RegisterReceptionRequest
            {
                PurchaseOrderId = order.Id,
                Lines = [new ReceptionLineRequest { PurchaseOrderLineId = order.Lines.Single().Id, Accepted = 12 }]
            }),
            HttpStatusCode.Created);

        Assert.Equal(PurchaseOrderStatus.Received, (await buyer.GetFromJsonAsync<PurchaseOrderResponse>($"api/compras/{order.Id}"))!.StatusCode);

        StockItemResponse stock = Assert.Single((await buyer.GetFromJsonAsync<PagedResponse<StockItemResponse>>(
            $"api/inventario/existencias?search={book.Code}"))!.Items);
        Assert.Equal(20m, stock.Available);
        Assert.Equal(StockStatus.Normal, stock.Status);
        Assert.Equal(38m, stock.AverageCost);
        Assert.Equal(760m, stock.TotalValue);

        // 7. Ajuste por producto dañado.
        AdjustmentReasonResponse damage = inventoryCatalogs.AdjustmentReasons.Single(reason => reason.Code == "DANIO");
        await OkAsync<AdjustmentResponse>(
            await buyer.PostAsJsonAsync("api/inventario/ajustes", new SaveAdjustmentRequest
            {
                ReasonId = damage.Id,
                WarehouseId = warehouse,
                Notes = "Tres ejemplares mojados en bodega.",
                Lines = [new AdjustmentLineRequest { BookId = book.Id, Quantity = 3 }]
            }),
            HttpStatusCode.Created);

        // 8. Kardex con los tres movimientos y el saldo acumulado.
        KardexResponse kardex = (await buyer.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book.Id}"))!;
        Assert.Equal([8m, 20m, 17m], kardex.Entries.Select(entry => entry.Balance).ToArray());
        Assert.Equal(20m, kardex.TotalIn);
        Assert.Equal(3m, kardex.TotalOut);
        Assert.Equal(17m, kardex.ClosingBalance);

        // 9. El costo de referencia del libro es el último costo de compra.
        Assert.Equal(38m, (await admin.GetFromJsonAsync<ProductResponse>($"api/productos/{book.Id}"))!.ReferenceCost);

        // 10. Sin órdenes abiertas el proveedor ya puede desactivarse.
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PatchAsJsonAsync($"api/proveedores/{supplier.Id}/estado", new ChangeSupplierStatusRequest { IsActive = false })).StatusCode);
    }
}
