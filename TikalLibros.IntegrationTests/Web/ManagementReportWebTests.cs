extern alias web;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using TikalLibros.Application.DTOs.Reports;
using web::TikalLibros.Web.Components.Pages.Reports;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Pantallas de los reportes de gestión.</summary>
public sealed class ManagementReportWebTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task CuentasPorCobrar_MuestraAntiguedadYFiltraVencidas()
    {
        await Session.SignInAsync(ValidLogin("Reportes.Ventas"));
        Handler.Respond = _ => Json(new ReceivablesResponse
        {
            AsOf = new DateOnly(2026, 10, 27),
            Total = 900m,
            Overdue = 400m,
            SalesCount = 2,
            CustomersCount = 1,
            Buckets = AgingBuckets.All.Select(name => new SalesByGroupRow { Name = name, Total = name == AgingBuckets.Days60 ? 400m : 0m }).ToList(),
            Customers = [new ReceivableCustomerRow { CustomerId = 3, Customer = "Librería Quetzal", Sales = 2, Balance = 900m, Overdue = 400m, MaxDaysOverdue = 40 }],
            Sales =
            [
                new ReceivableSaleRow { SaleId = 8, Number = "VEN-000008", Customer = "Librería Quetzal", Balance = 400m, DaysOverdue = 40, Bucket = AgingBuckets.Days60 },
                new ReceivableSaleRow { SaleId = 9, Number = "VEN-000009", Customer = "Librería Quetzal", Balance = 500m, DaysOverdue = 0, Bucket = AgingBuckets.Current }
            ]
        });

        IRenderedComponent<ReceivablesReport> page = Render<ReceivablesReport>();

        page.WaitForAssertion(() => Assert.Contains("VEN-000008", page.Markup));
        Assert.Contains("40 días", page.Markup);
        Assert.Contains("Q 900.00", page.Markup);
        Assert.Equal("/api/reportes/cuentas-por-cobrar", Handler.LastPathAndQuery);

        page.Find("input[type='checkbox']").Change(true);
        page.WaitForAssertion(() => Assert.Equal("/api/reportes/cuentas-por-cobrar?onlyOverdue=true", Handler.LastPathAndQuery));
    }

    [Fact]
    public async Task Rentabilidad_MuestraMargenYPorcentaje()
    {
        await Session.SignInAsync(ValidLogin("Reportes.Rentabilidad"));
        Handler.Respond = _ => Json(new ProfitabilityReportResponse
        {
            From = new DateOnly(2026, 10, 1),
            To = new DateOnly(2026, 10, 27),
            Units = 10,
            Revenue = 1000m,
            Cost = 600m,
            Margin = 400m,
            MarginPercent = 40m,
            ByBook = [new ProfitRow { Id = 1, Code = "LIB-0001", Name = "Rayuela", Units = 10, Revenue = 1000m, Cost = 600m, Margin = 400m, MarginPercent = 40m }]
        });

        IRenderedComponent<ProfitabilityReport> page = Render<ProfitabilityReport>();

        page.WaitForAssertion(() => Assert.Contains("Rayuela", page.Markup));
        Assert.Contains("40 % del ingreso", page.Markup);
        Assert.Contains("Q 400.00", page.Markup);
    }
}
