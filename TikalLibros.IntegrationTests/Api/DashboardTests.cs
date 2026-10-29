using System.Net;
using System.Net.Http.Json;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Indicadores del inicio: secciones según permisos y cifras coherentes con los reportes.</summary>
public sealed class DashboardTests : SalesTestBase
{
    public DashboardTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<DashboardResponse> DashboardAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<DashboardResponse>("api/dashboard"))!;

    [Fact]
    public async Task Administrador_VeTodasLasSeccionesYLasVentasDeHoy()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 80m, stock: 3);

        DashboardResponse before = await DashboardAsync(admin);
        await OkAsync<SaleResponse>(await SellAsync(admin, Sale(null, [await CashAsync(admin, 160m)], (book, 2, 0m))), HttpStatusCode.Created);
        DashboardResponse after = await DashboardAsync(admin);

        Assert.NotNull(after.Sales);
        Assert.NotNull(after.Profit);
        Assert.NotNull(after.Receivables);
        Assert.NotNull(after.Inventory);
        Assert.NotNull(after.Cash);
        Assert.NotNull(after.Orders);
        Assert.NotNull(after.Purchases);

        Assert.Equal((160m, 1), (after.Sales!.TodayTotal - before.Sales!.TodayTotal, after.Sales.TodayCount - before.Sales.TodayCount));
        Assert.Equal(160m, after.Sales.MonthTotal - before.Sales.MonthTotal);
        Assert.Equal(30, after.Sales.Last30Days.Count);
        Assert.Equal(12, after.Sales.Last12Months.Count);
        Assert.Equal(after.Sales.MonthTotal, after.Sales.Last12Months.Last().Total);
        Assert.Contains(after.Sales.TopBooks, row => row.Count >= 2);
        Assert.NotNull(after.Inventory!.Value);
        Assert.True(after.Cash!.OpenSessions >= 1);
        Assert.True(after.Cash.TodayIncome - before.Cash!.TodayIncome >= 160m);
    }

    [Fact]
    public async Task Secciones_DependenDeLosPermisos()
    {
        (HttpClient warehouse, _) = await EmployeeClientAsync(InventoryPermissions.View);
        DashboardResponse inventoryOnly = await DashboardAsync(warehouse);

        Assert.NotNull(inventoryOnly.Inventory);
        Assert.Null(inventoryOnly.Inventory!.Value); // sin permiso de costos no se envía el valor
        Assert.Null(inventoryOnly.Sales);
        Assert.Null(inventoryOnly.Profit);
        Assert.Null(inventoryOnly.Cash);

        (HttpClient nobody, _) = await EmployeeClientAsync(CustomerPermissions.View);
        DashboardResponse empty = await DashboardAsync(nobody);
        Assert.True(empty is { Sales: null, Profit: null, Receivables: null, Inventory: null, Cash: null, Orders: null, Purchases: null });

        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync("api/dashboard")).StatusCode);
    }
}
