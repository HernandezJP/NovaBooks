extern alias web;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using TikalLibros.Application.DTOs.Purchasing;
using web::TikalLibros.Web.Components.Pages.Purchases;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Registro de devoluciones al proveedor desde la recepción.</summary>
public sealed class SupplierReturnWebTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value) };

    private static readonly ReturnableReceptionResponse Reception = new()
    {
        ReceptionId = 12,
        ReceptionNumber = "REC-000012",
        Supplier = "Distribuidora Petén",
        Warehouse = "Bodega principal",
        Lines =
        [
            new ReturnableLineResponse { ReceptionLineId = 40, BookId = 3, BookCode = "LIB-0003", Title = "Hombres de maíz", LotNumber = "REC-000012", Accepted = 10, Returnable = 4, UnitCost = 50m }
        ]
    };

    [Fact]
    public async Task Nueva_ValidaLoDevolvibleYEnviaLaDevolucion()
    {
        await Session.SignInAsync(ValidLogin("Compras.Devolver", "Compras.Ver"));
        string? sentBody = null;

        Handler.Respond = request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                sentBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Json(new SupplierReturnResponse { Id = 9, Number = "MOV-000090", Total = 150m }, HttpStatusCode.Created);
            }

            return Json(Reception);
        };

        Navigation.NavigateTo("/compras/devoluciones/nueva?recepcion=12");
        IRenderedComponent<SupplierReturnNew> page = Render<SupplierReturnNew>();
        page.WaitForAssertion(() => Assert.Contains("Hombres de maíz", page.Markup));
        Assert.Equal("/api/devoluciones-proveedor/recepciones/12", Handler.LastPath);

        page.Find("input[aria-label='Unidades a devolver de Hombres de maíz']").Input("3");
        page.Find("textarea").Change("Ejemplares con páginas en blanco");
        page.WaitForAssertion(() => Assert.Contains("150.00", page.Find(".doc-totals").TextContent));

        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.NotNull(sentBody));
        using JsonDocument body = JsonDocument.Parse(sentBody!);
        Assert.Equal(12, body.RootElement.GetProperty("receptionId").GetInt32());
        JsonElement line = body.RootElement.GetProperty("lines")[0];
        Assert.Equal((40, 3), (line.GetProperty("receptionLineId").GetInt32(), line.GetProperty("quantity").GetInt32()));
        Assert.Equal("Ejemplares con páginas en blanco", body.RootElement.GetProperty("reason").GetString());
    }
}
