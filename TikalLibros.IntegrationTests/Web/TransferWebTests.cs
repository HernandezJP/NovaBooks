extern alias web;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using MudBlazor;
using TikalLibros.Application.DTOs.Inventory;
using web::TikalLibros.Web.Components.Pages.Inventory;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Pantalla de detalle de traslados y su anulación.</summary>
public sealed class TransferWebTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value) };

    private static MovementResponse Transfer(bool annulled) => new()
    {
        Id = 15,
        Number = "MOV-000015",
        TypeCode = "TRANSFERENCIA",
        Type = "Transferencia entre almacenes",
        SourceWarehouse = "Bodega principal",
        TargetWarehouse = "Bodega Xela",
        Annulled = annulled,
        AnnulmentReason = annulled ? "Se envió a la bodega equivocada" : null,
        Lines = [new MovementLineResponse { BookId = 3, BookCode = "LIB-0003", Title = "Hombres de maíz", LotNumber = "REC-000004", Quantity = 4 }]
    };

    [Fact]
    public async Task Detalle_MuestraOrigenDestinoYPermiteAnular()
    {
        await Session.SignInAsync(ValidLogin("Inventario.Transferir"));
        Handler.Respond = request => Json(Transfer(annulled: request.Method == HttpMethod.Post));

        IRenderedComponent<MudDialogProvider> provider = Render<MudDialogProvider>();
        IRenderedComponent<TransferDetail> page = Render<TransferDetail>(parameters => parameters.Add(p => p.Id, 15));

        page.WaitForAssertion(() => Assert.Contains("Bodega Xela", page.Markup));
        Assert.Contains("Bodega principal", page.Markup);
        Assert.Contains("REC-000004", page.Markup);
        Assert.Equal("/api/inventario/traslados/15", Handler.LastPath);

        // El clic queda esperando el diálogo del motivo; no se espera aquí.
        _ = page.FindAll("button").Single(button => button.TextContent.Contains("Anular traslado")).ClickAsync(new());
        provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll("textarea")));
        provider.Find("textarea").Input("Se envió a la bodega equivocada");
        provider.Find("#reason-form").Submit();

        page.WaitForAssertion(() => Assert.Contains("Anulado", page.Markup));
        Assert.Equal("/api/inventario/traslados/15/anular", Handler.LastPath);
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Contains("Anular traslado"));
    }
}
