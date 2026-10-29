extern alias web;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Reports;
using web::TikalLibros.Web.Components.Pages;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Inicio con indicadores y gráficos según lo que entrega la API.</summary>
public sealed class DashboardWebTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task Inicio_MuestraIndicadoresYGraficosDeVentas()
    {
        await Session.SignInAsync(ValidLogin("Reportes.Ventas"));
        DateOnly today = new(2026, 10, 27);

        Handler.Respond = request => request.RequestUri!.AbsolutePath == "/api/dashboard"
            ? Json(new DashboardResponse
            {
                Sales = new DashboardSales
                {
                    TodayTotal = 1250m,
                    TodayCount = 7,
                    MonthTotal = 18400m,
                    MonthChangePercent = 12.5m,
                    Last30Days = Enumerable.Range(0, 30).Select(offset => new SalesByDayRow { Date = today.AddDays(offset - 29), Total = offset * 10m }).ToList(),
                    Last12Months = Enumerable.Range(0, 12).Select(offset => new MonthTotalRow { Year = 2026, Month = offset + 1, Total = 1000m + offset }).ToList(),
                    PaymentMethods = [new SalesByGroupRow { Name = "Efectivo", Total = 900m, Count = 5 }, new SalesByGroupRow { Name = "Tarjeta", Total = 350m, Count = 2 }],
                    TopBooks = [new SalesByGroupRow { Name = "Rayuela", Count = 4, Total = 480m }]
                },
                Receivables = new DashboardReceivables { Total = 600m, Overdue = 200m, Customers = 2, Buckets = [new SalesByGroupRow { Name = AgingBuckets.Current, Total = 400m }] }
            })
            : Json(new PagedResponse<object>());

        IRenderedComponent<Home> page = Render<Home>();

        page.WaitForAssertion(() => Assert.Contains("Q 1,250.00", page.Markup));
        Assert.Contains("Sube 12.5 %", page.Markup);
        Assert.Contains("Vencido Q 200.00", page.Markup);
        Assert.Equal(3, page.FindAll(".stat-card").Count);

        // Cada gráfico tiene su tabla equivalente para lectores de pantalla.
        Assert.Contains(page.FindAll(".sr-only table caption"), caption => caption.TextContent == "Ventas por día de los últimos 30 días");
        Assert.Contains(page.FindAll(".sr-only table caption"), caption => caption.TextContent == "Cobros del mes por método de pago");
        Assert.NotEmpty(page.FindAll(".mud-chart"));
    }

    [Fact]
    public async Task Inicio_MuestraComoMaximoCuatroTarjetasPorPrioridad()
    {
        await Session.SignInAsync(ValidLogin("Reportes.Ventas", "Pedidos.Aprobar", "Caja.Ver", "Inventario.Ver"));
        Handler.Respond = _ => Json(new DashboardResponse
        {
            Sales = new DashboardSales { TodayTotal = 10m, MonthTotal = 20m },
            Receivables = new DashboardReceivables { Total = 30m },
            Orders = new DashboardOrders { PendingApproval = 2 },
            Cash = new DashboardCash { OpenSessions = 1 },
            Inventory = new DashboardInventory { OutOfStock = 1 }
        });

        IRenderedComponent<Home> page = Render<Home>();

        page.WaitForAssertion(() => Assert.Equal(4, page.FindAll(".stat-card").Count));
        Assert.Contains("Pedidos por aprobar", page.Markup);
        Assert.DoesNotContain("Caja de hoy", page.Markup);
        Assert.DoesNotContain("Alertas de inventario", page.Markup);
    }

    [Fact]
    public async Task Inicio_SinSecciones_NoMuestraTarjetasNiGraficos()
    {
        await Session.SignInAsync(ValidLogin("Clientes.Ver", "Clientes.Crear"));
        Handler.Respond = _ => Json(new DashboardResponse());

        IRenderedComponent<Home> page = Render<Home>();

        page.WaitForAssertion(() => Assert.Equal("/api/dashboard", Handler.LastPath));
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".state--loading")));
        Assert.Empty(page.FindAll(".stat-card"));
        Assert.Empty(page.FindAll(".mud-chart"));
        Assert.DoesNotContain("Su cuenta", page.Markup);
        Assert.DoesNotContain("Accesos rápidos", page.Markup);
    }
}
