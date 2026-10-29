using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Audit;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Reportes iniciales y consulta de auditoría.</summary>
public sealed class ReportsTests : SalesTestBase
{
    public ReportsTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<SalesReportResponse> SalesReportAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<SalesReportResponse>("api/reportes/ventas"))!;

    [Fact]
    public async Task VentasPorPeriodo_SumaVentasYCobrosSinContarAnuladas()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m, stock: 10);
        SalesReportResponse before = await SalesReportAsync(admin);

        await OkAsync<SaleResponse>(await SellAsync(admin, Sale(null, [await CashAsync(admin, 200m)], (book, 2, 0m))), HttpStatusCode.Created);
        await OkAsync<SaleResponse>(await SellAsync(admin, Sale(null, [await CardAsync(admin, 90m)], (book, 1, 10m))), HttpStatusCode.Created);
        SaleResponse annulled = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 100m)], (book, 1, 0m))), HttpStatusCode.Created);
        await OkAsync<SaleResponse>(await admin.PostAsJsonAsync($"api/ventas/{annulled.Id}/anular", new AnnulSaleRequest { Reason = "Venta de prueba anulada" }));

        SalesReportResponse after = await SalesReportAsync(admin);

        Assert.Equal(before.SalesCount + 2, after.SalesCount);
        Assert.Equal(before.AnnulledCount + 1, after.AnnulledCount);
        Assert.Equal(before.Total + 290m, after.Total);
        Assert.Equal(before.Discount + 10m, after.Discount);
        Assert.Equal(before.Units + 3m, after.Units);
        Assert.Equal(30, after.ByDay.Count);
        Assert.Equal(BusinessTime.Today, after.ByDay.Last().Date);
        Assert.Equal(before.ByDay.Last().Total + 290m, after.ByDay.Last().Total);

        decimal Cash(SalesReportResponse report) => report.ByPaymentMethod.FirstOrDefault(item => item.Name == "Efectivo")?.Total ?? 0m;
        decimal Card(SalesReportResponse report) => report.ByPaymentMethod.FirstOrDefault(item => item.Name == "Tarjeta")?.Total ?? 0m;
        Assert.Equal(Cash(before) + 200m, Cash(after));
        Assert.Equal(Card(before) + 90m, Card(after));
    }

    [Theory]
    [InlineData("from=2026-10-20&to=2026-10-01", "posterior a la final")]
    [InlineData("from=2024-01-01&to=2026-10-01", "no puede superar los 366 días")]
    public async Task Reporte_PeriodoInvalido_Devuelve400(string query, string message)
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.GetAsync($"api/reportes/ventas?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(message, await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task LibrosMasVendidos_OrdenaPorUnidadesOIngresosYMuestraMargenConPermiso()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int many = await CreateBookAsync(admin, price: 56m, stock: 10);   // costo de compra Q50
        int expensive = await CreateBookAsync(admin, price: 448m, stock: 10);

        await OkAsync<SaleResponse>(await SellAsync(admin, Sale(null, [await CashAsync(admin, 280m)], (many, 5, 0m))), HttpStatusCode.Created);
        await OkAsync<SaleResponse>(await SellAsync(admin, Sale(null, [await CashAsync(admin, 896m)], (expensive, 2, 0m))), HttpStatusCode.Created);

        TopBooksResponse byUnits = (await admin.GetFromJsonAsync<TopBooksResponse>("api/reportes/libros-mas-vendidos?top=100"))!;
        TopBooksResponse byRevenue = (await admin.GetFromJsonAsync<TopBooksResponse>("api/reportes/libros-mas-vendidos?top=100&orderBy=revenue"))!;

        int Rank(TopBooksResponse report, int book) => report.Rows.Single(row => row.BookId == book).Rank;
        Assert.True(Rank(byUnits, many) < Rank(byUnits, expensive));
        Assert.True(Rank(byRevenue, expensive) < Rank(byRevenue, many));

        TopBookRow row = byUnits.Rows.Single(item => item.BookId == many);
        Assert.True(byUnits.IncludesCosts);
        Assert.Equal((5m, 280m, 250m), (row.Units, row.Revenue, row.Cost!.Value));
        Assert.Equal(0m, row.Margin); // 280 − 30 de IVA − 250 de costo

        (string userName, _) = await CreateUserWithPermissionsAsync(ReportPermissions.Sales);
        HttpClient seller = await LoginAsync(userName, TestPassword);
        TopBooksResponse withoutCosts = (await seller.GetFromJsonAsync<TopBooksResponse>("api/reportes/libros-mas-vendidos?top=100"))!;
        Assert.False(withoutCosts.IncludesCosts);
        Assert.All(withoutCosts.Rows, item => Assert.Null(item.Cost));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("api/reportes/libros-mas-vendidos?top=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("api/reportes/libros-mas-vendidos?orderBy=precio")).StatusCode);
    }

    [Fact]
    public async Task InventarioValorizado_UsaElCostoPromedioDeLosLotes()
    {
        HttpClient admin = await AdminAsync();
        int book = await CreateBookAsync(admin, price: 100m);
        await AddStockAsync(admin, book, 4, cost: 10m);
        await AddStockAsync(admin, book, 6, cost: 20m);
        int empty = await CreateBookAsync(admin);
        await AddStockAsync(admin, empty, 1, cost: 10m);
        AdjustmentReasonResponse damage = (await admin.GetFromJsonAsync<InventoryCatalogsResponse>("api/inventario/catalogos"))!
            .AdjustmentReasons.First(reason => reason.Code == "DANIO");
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/inventario/ajustes", new SaveAdjustmentRequest
        {
            ReasonId = damage.Id,
            Notes = "Ejemplar dañado en bodega",
            Lines = [new AdjustmentLineRequest { BookId = empty, Quantity = 1 }]
        })).StatusCode);

        ProductResponse product = (await admin.GetFromJsonAsync<ProductResponse>($"api/productos/{book}"))!;
        InventoryValuationResponse report = (await admin.GetFromJsonAsync<InventoryValuationResponse>(
            $"api/reportes/inventario-valorizado?search={product.Code}"))!;

        InventoryValuationRow row = Assert.Single(report.Rows);
        Assert.Equal((10m, 16m, 160m), (row.Available, row.AverageCost, row.Value));
        Assert.Equal(160m, report.TotalValue);

        InventoryValuationResponse outOfStock = (await admin.GetFromJsonAsync<InventoryValuationResponse>(
            $"api/reportes/inventario-valorizado?status={StockStatus.OutOfStock}"))!;
        Assert.Contains(outOfStock.Rows, item => item.BookId == empty);
        Assert.DoesNotContain(outOfStock.Rows, item => item.BookId == book);

        (string userName, _) = await CreateUserWithPermissionsAsync(ReportPermissions.Sales);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await LoginAsync(userName, TestPassword)).GetAsync("api/reportes/inventario-valorizado")).StatusCode);
    }

    [Fact]
    public async Task PedidosPendientes_MuestraPorAprobarYReservadosPorVencer()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 10);

        OrderResponse pending = await CreateOrderAsync(admin, Order(customer, (book, 1, 30m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, pending.Id));
        OrderResponse reserved = await CreateOrderAsync(admin, Order(customer, (book, 2, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, reserved.Id));

        await Factory.QueryAsync(async context =>
        {
            var order = await context.PedidosCliente.SingleAsync(item => item.PED_PedidoCliente == reserved.Id);
            order.PED_FechaVencimientoReserva = DateTime.UtcNow.AddHours(5);
            await context.SaveChangesAsync();
            return true;
        });

        PendingOrdersResponse report = (await admin.GetFromJsonAsync<PendingOrdersResponse>("api/reportes/pedidos-pendientes"))!;

        PendingOrderRow pendingRow = report.Rows.Single(row => row.Id == pending.Id);
        PendingOrderRow reservedRow = report.Rows.Single(row => row.Id == reserved.Id);
        Assert.Equal(OrderStatus.PendingApproval, pendingRow.StatusCode);
        Assert.True(reservedRow.ExpiresSoon);
        Assert.True(report.ExpiringSoon >= 1);
        Assert.Equal(OrderStatus.PendingApproval, report.Summary.First().StatusCode); // primero lo que espera decisión
        Assert.True(report.Rows.ToList().IndexOf(pendingRow) < report.Rows.ToList().IndexOf(reservedRow));
    }

    [Fact]
    public async Task Auditoria_SeConsultaConFiltrosDetalleYAccesos()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, stock: 3);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 1, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));
        await PostLoginAsync(Anonymous(), TikalLibrosApiFactory.AdminUser, "Equivocada123*");

        PagedResponse<AuditEntryResponse> entries = (await admin.GetFromJsonAsync<PagedResponse<AuditEntryResponse>>(
            $"api/auditoria?entity=PedidoCliente&entityId={order.Id}"))!;
        Assert.Equal(["CONFIRMAR", "CREAR"], entries.Items.Select(item => item.Action));
        Assert.All(entries.Items, item => Assert.Equal(TikalLibrosApiFactory.AdminUser, item.UserName));

        AuditDetailResponse detail = (await admin.GetFromJsonAsync<AuditDetailResponse>($"api/auditoria/{entries.Items.First().Id}"))!;
        Assert.Contains(detail.Changes, change => change.Field == "Estado" && change.Before == "Borrador" && change.After == "Reservado");

        PagedResponse<AuditEntryResponse> confirmations = (await admin.GetFromJsonAsync<PagedResponse<AuditEntryResponse>>(
            $"api/auditoria?action=CONFIRMAR&search={order.Number}"))!;
        Assert.Equal(order.Id.ToString(), Assert.Single(confirmations.Items).EntityId);

        AuditCatalogsResponse catalogs = (await admin.GetFromJsonAsync<AuditCatalogsResponse>("api/auditoria/catalogos"))!;
        Assert.Contains("PedidoCliente", catalogs.Entities);

        PagedResponse<AccessLogResponse> failed = (await admin.GetFromJsonAsync<PagedResponse<AccessLogResponse>>(
            $"api/auditoria/accesos?userName={TikalLibrosApiFactory.AdminUser}&succeeded=false"))!;
        Assert.Contains(failed.Items, item => item.FailureReason == "Usuario o contraseña incorrectos.");

        (string userName, _) = await CreateUserWithPermissionsAsync(ReportPermissions.Sales);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await LoginAsync(userName, TestPassword)).GetAsync("api/auditoria")).StatusCode);
    }
}
