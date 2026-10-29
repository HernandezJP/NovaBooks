using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Centro de alertas: tipos según permisos, conteos y orden por severidad.</summary>
public sealed class AlertsTests : SalesTestBase
{
    public AlertsTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<AlertsResponse> AlertsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<AlertsResponse>("api/alertas"))!;

    [Fact]
    public async Task Administrador_VeCreditosVencidosEInventarioConLosConteosDeSusPantallas()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin, creditLimit: 2000m, creditDays: 15);
        int book = await CreateBookAsync(admin, price: 90m, stock: 1);

        DirectSaleRequest request = Sale(customer, [], (book, 1, 0m));
        request.SaleType = SaleType.Credit;
        SaleResponse sale = await OkAsync<SaleResponse>(await SellAsync(admin, request), HttpStatusCode.Created);

        await Factory.QueryAsync(async context =>
        {
            var entity = await context.Ventas.SingleAsync(item => item.VEN_Venta == sale.Id);
            entity.VEN_FechaVencimientoCredito = BusinessTime.Today.AddDays(-3);
            return await context.SaveChangesAsync();
        });

        AlertsResponse alerts = await AlertsAsync(admin);

        AlertItem credit = alerts.Items.Single(item => item.Code == "CREDITOS_VENCIDOS");
        Assert.Equal((AlertSeverity.Critical, "/reportes/cuentas-por-cobrar"), (credit.Severity, credit.Href));
        Assert.True(credit.Count >= 1);

        // El libro vendido completo queda agotado; el conteo coincide con la pantalla de alertas.
        StockAlertSummaryResponse summary = (await admin.GetFromJsonAsync<StockAlertSummaryResponse>("api/inventario/alertas/resumen"))!;
        Assert.Equal(summary.OutOfStock, alerts.Items.Single(item => item.Code == "INVENTARIO_AGOTADO").Count);

        // Las críticas van primero y el total suma todo.
        Assert.Equal(AlertSeverity.Critical, alerts.Items[0].Severity);
        Assert.Equal(alerts.Items.Sum(item => item.Count), alerts.Total);
        Assert.Equal(alerts.Items.Where(item => item.Severity == AlertSeverity.Critical).Sum(item => item.Count), alerts.Critical);
        Assert.All(alerts.Items, item => Assert.True(item.Count > 0));
    }

    [Fact]
    public async Task CajaAbiertaDesdeAyer_AlertaAlSupervisorYAlCajero()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync(CashPermissions.Open);
        int sessionId = (await EnsureCashAsync(cashier)).Id;

        Assert.DoesNotContain((await AlertsAsync(cashier)).Items, item => item.Code == "MI_CAJA_SIN_CERRAR");

        await Factory.QueryAsync(async context =>
        {
            var session = await context.AperturasCaja.SingleAsync(item => item.ACA_AperturaCaja == sessionId);
            session.ACA_FechaApertura = DateTime.UtcNow.AddDays(-1).AddHours(-2);
            return await context.SaveChangesAsync();
        });

        AlertItem own = (await AlertsAsync(cashier)).Items.Single(item => item.Code == "MI_CAJA_SIN_CERRAR");
        Assert.Equal(("/caja", 1), (own.Href, own.Count));

        AlertItem all = (await AlertsAsync(admin)).Items.Single(item => item.Code == "CAJAS_SIN_CERRAR");
        Assert.True(all.Count >= 1);
        Assert.DoesNotContain((await AlertsAsync(admin)).Items, item => item.Code == "MI_CAJA_SIN_CERRAR");
    }

    [Fact]
    public async Task PedidosPorAprobar_SoloParaQuienPuedeAprobar()
    {
        HttpClient admin = await AdminAsync();
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m, stock: 2);

        // Un descuento mayor al permitido deja el pedido por aprobar.
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 1, 30m)));
        Assert.Equal(HttpStatusCode.OK, (await ConfirmAsync(admin, order.Id)).StatusCode);

        AlertsResponse supervisor = await AlertsAsync(admin);
        Assert.Contains(supervisor.Items, item => item.Code == "PEDIDOS_POR_APROBAR" && item.Href == "/pedidos/aprobaciones");

        (HttpClient seller, _) = await EmployeeClientAsync(OrderPermissions.View, OrderPermissions.Create);
        Assert.DoesNotContain((await AlertsAsync(seller)).Items, item => item.Code == "PEDIDOS_POR_APROBAR");
    }

    [Fact]
    public async Task SinPermisosRelacionados_NoHayAlertas_YAnonimoRecibe401()
    {
        (HttpClient customers, _) = await EmployeeClientAsync(CustomerPermissions.View);

        AlertsResponse alerts = await AlertsAsync(customers);

        Assert.Empty(alerts.Items);
        Assert.Equal(0, alerts.Total);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync("api/alertas")).StatusCode);
    }
}
