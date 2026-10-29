extern alias web;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TikalLibros.Application.DTOs.Reports;
using web::TikalLibros.Web.Components.Layout;
using web::TikalLibros.Web.Services.Session;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Centro de alertas: estado compartido y lista del panel lateral.</summary>
public sealed class AlertCenterWebTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task Lista_MuestraSeveridadConteoYEnlace()
    {
        await Session.SignInAsync(ValidLogin("Inventario.Ver", "Reportes.Ventas"));
        Handler.Respond = _ => Json(new AlertsResponse
        {
            GeneratedAt = DateTime.UtcNow,
            Total = 5,
            Critical = 3,
            Items =
            [
                new AlertItem { Code = "CREDITOS_VENCIDOS", Severity = AlertSeverity.Critical, Title = "Créditos vencidos", Count = 3, Message = "3 venta(s)", Href = "/reportes/cuentas-por-cobrar" },
                new AlertItem { Code = "INVENTARIO_BAJO_MINIMO", Severity = AlertSeverity.Warning, Title = "Existencia bajo el mínimo", Count = 2, Message = "2 libro(s)", Href = "/inventario/alertas" }
            ]
        });

        AlertState state = Services.GetRequiredService<AlertState>();
        await state.RefreshAsync(force: true);
        IRenderedComponent<AlertList> list = Render<AlertList>();

        Assert.Equal(5, state.Total);
        Assert.Equal(2, list.FindAll(".alert-item").Count);
        Assert.Contains("alert-item--critica", list.FindAll(".alert-item")[0].ClassName);
        Assert.Equal("/reportes/cuentas-por-cobrar", list.FindAll(".alert-item")[0].GetAttribute("href"));
        Assert.Contains("Crítica", list.Markup);
        Assert.Contains("Advertencia", list.Markup);
        Assert.Equal("/api/alertas", Handler.LastPath);
    }

    [Fact]
    public async Task SinAlertas_MuestraTodoEnOrden_YNoConsultaRecienteDosVeces()
    {
        await Session.SignInAsync(ValidLogin("Clientes.Ver"));
        int calls = 0;
        Handler.Respond = _ =>
        {
            calls++;
            return Json(new AlertsResponse { GeneratedAt = DateTime.UtcNow });
        };

        AlertState state = Services.GetRequiredService<AlertState>();
        await state.RefreshAsync(force: true);
        int afterFirst = calls;
        await state.RefreshAsync(); // dentro del minuto: no vuelve a consultar
        IRenderedComponent<AlertList> list = Render<AlertList>();

        Assert.Contains("Todo en orden", list.Markup);
        Assert.Equal(afterFirst, calls);
        Assert.Equal(0, state.Total);
    }
}
