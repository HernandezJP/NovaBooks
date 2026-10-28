extern alias web;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Sales;
using web::TikalLibros.Web.Components.Pages.Returns;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Registro de devoluciones desde la venta.</summary>
public sealed class ReturnWebTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value) };

    private static readonly ReturnCatalogsResponse Catalogs = new()
    {
        Reasons =
        [
            new ReturnReasonResponse { Id = 2, Code = "INCORRECTO", Name = "Producto incorrecto", Restocks = true, RequiresNotes = true },
            new ReturnReasonResponse { Id = 1, Code = "DEFECTUOSO", Name = "Producto defectuoso", Restocks = false, RequiresNotes = true }
        ],
        RefundMethods = [new CatalogOptionResponse { Id = 1, Code = "EFECTIVO", Name = "Efectivo" }]
    };

    // Venta al crédito de Q300 con saldo Q100: lo devuelto se abona primero al saldo.
    private static readonly SaleResponse Sale = new()
    {
        Id = 7,
        Number = "VEN-000007",
        Customer = "Ana Morales",
        Total = 300m,
        Paid = 200m,
        Balance = 100m,
        CanReturn = true,
        Lines = [new SaleLineResponse { LineId = 31, BookId = 4, BookCode = "LIB-0004", Title = "El Señor Presidente", Quantity = 3, Total = 300m }]
    };

    [Fact]
    public async Task Nueva_CalculaAbonoYReembolsoYExigeMotivo()
    {
        await Session.SignInAsync(ValidLogin("Ventas.Devolver", "Ventas.Ver"));
        string? sentBody = null;

        Handler.Respond = request =>
        {
            string path = request.RequestUri!.AbsolutePath;

            if (request.Method == HttpMethod.Post)
            {
                sentBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Json(new ReturnResponse { Id = 5, Number = "DEV-000005", Total = 200m, CreditApplied = 100m, Refunded = 100m }, HttpStatusCode.Created);
            }

            return path.EndsWith("catalogos") ? Json(Catalogs) : Json(Sale);
        };

        Navigation.NavigateTo("/devoluciones/nueva?venta=7");
        IRenderedComponent<ReturnNew> page = Render<ReturnNew>();
        page.WaitForAssertion(() => Assert.Contains("El Señor Presidente", page.Markup));

        page.Find("input[aria-label='Unidades a devolver de El Señor Presidente']").Input("2");
        page.WaitForAssertion(() => Assert.Contains("200.00", page.Find(".doc-totals").TextContent));

        // Sin motivo no se envía.
        page.FindAll("button").Single(button => button.TextContent.Contains("Registrar devolución")).Click();
        page.WaitForAssertion(() => Assert.Contains("Seleccione el motivo.", page.Markup));
        Assert.Null(sentBody);

        Assert.Contains("Abono al saldo", page.Markup);
        Assert.Contains("100.00", page.Find(".doc-totals").TextContent);
    }

    [Fact]
    public async Task Nueva_VentaSinUnidadesPorDevolver_MuestraAviso()
    {
        await Session.SignInAsync(ValidLogin("Ventas.Devolver", "Ventas.Ver"));
        SaleResponse returned = new() { Id = 8, Number = "VEN-000008", CanReturn = false, Lines = [] };

        Handler.Respond = request => request.RequestUri!.AbsolutePath.EndsWith("catalogos") ? Json(Catalogs) : Json(returned);

        Navigation.NavigateTo("/devoluciones/nueva?venta=8");
        IRenderedComponent<ReturnNew> page = Render<ReturnNew>();

        page.WaitForAssertion(() => Assert.Contains("ya se devolvieron", page.Markup));
        Assert.DoesNotContain("Libros a devolver", page.Markup);
    }
}
