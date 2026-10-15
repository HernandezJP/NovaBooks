extern alias web;

using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.Options;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Purchasing;
using web::TikalLibros.Web.Components.Shared;
using web::TikalLibros.Web.Services.Api;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Campo de fecha usado en todos los formularios.</summary>
public sealed class DateFieldTests : WebTestContext
{
    [Fact]
    public void FechaInexistente_MuestraMensajeDeFechaNoValida()
    {
        IRenderedComponent<DateField> component = Render<DateField>(parameters => parameters.Add(p => p.Label, "Fecha"));

        component.Find("input").Change("31/02/2026");

        Assert.Contains("Fecha no válida", component.Markup);
    }

    [Fact]
    public void FechaFueraDeRango_AvisaYLaConservaAunqueElPadreNoSeHayaActualizado()
    {
        DateTime? received = null;
        IRenderedComponent<DateField> component = Render<DateField>(parameters => parameters
            .Add(p => p.MaxDate, new DateTime(2008, 10, 14))
            .Add(p => p.ValueChanged, (DateTime? value) => received = value));

        component.Find("input").Change("01/01/2020");

        Assert.Equal(new DateTime(2020, 1, 1), received);
        Assert.Contains("no puede ser posterior al 14/10/2008", component.Markup);
        // El parámetro Value sigue en null (el padre no se volvió a dibujar):
        // el campo no debe borrar lo escrito.
        Assert.Equal("01/01/2020", component.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void FechaAnteriorAlMinimo_Avisa()
    {
        IRenderedComponent<DateField> component = Render<DateField>(parameters => parameters
            .Add(p => p.MinDate, new DateTime(2026, 10, 1)));

        component.Find("input").Change("30/09/2026");

        Assert.Contains("no puede ser anterior al 01/10/2026", component.Markup);
    }

    [Fact]
    public void FechaValida_NoMuestraErrorYUsaFormatoDiaMesAnio()
    {
        DateTime? received = null;
        IRenderedComponent<DateField> component = Render<DateField>(parameters => parameters
            .Add(p => p.Value, new DateTime(2026, 3, 5))
            .Add(p => p.ValueChanged, (DateTime? value) => received = value));

        Assert.Equal("05/03/2026", component.Find("input").GetAttribute("value"));

        component.Find("input").Change("15/03/1990");

        Assert.Equal(new DateTime(1990, 3, 15), received);
        Assert.DoesNotContain("mud-input-error", component.Markup);
    }

    [Fact]
    public void Campo_UsaFiltroDeFechaYPlaceholder()
    {
        IRenderedComponent<DateField> component = Render<DateField>();

        Assert.Contains("tl-filter-date", component.Markup);
        Assert.Equal("dd/mm/aaaa", component.Find("input").GetAttribute("placeholder"));
    }

    [Fact]
    public void SoloLectura_NoPermiteEscribir()
    {
        IRenderedComponent<DateField> component = Render<DateField>(parameters => parameters.Add(p => p.ReadOnly, true));

        Assert.True(component.Find("input").HasAttribute("readonly"));
    }
}

public sealed class DocumentStatusTests : WebTestContext
{
    [Theory]
    [InlineData("BORRADOR", "status-pill--neutral")]
    [InlineData("EMITIDA", "status-pill--info")]
    [InlineData("PARCIAL", "status-pill--warning")]
    [InlineData("RECIBIDA", "status-pill--active")]
    [InlineData("CANCELADA", "status-pill--danger")]
    [InlineData("ANULADA", "status-pill--danger")]
    [InlineData("AGOTADO", "status-pill--danger")]
    [InlineData("BAJO_MINIMO", "status-pill--warning")]
    [InlineData("SOBRE_MAXIMO", "status-pill--info")]
    [InlineData("NORMAL", "status-pill--active")]
    public void Estado_UsaElColorCorrespondiente(string code, string css)
    {
        IRenderedComponent<DocumentStatus> component = Render<DocumentStatus>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Text, "Texto"));

        Assert.Contains(css, component.Find("span").ClassName);
        Assert.Equal("Texto", component.Find("span").TextContent);
    }
}

/// <summary>Las URL que arman los clientes de compras e inventario.</summary>
public sealed class OperationsApiTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task Compras_EnviaFiltrosEnLaConsulta()
    {
        Handler.Respond = _ => Json(new PagedResponse<PurchaseOrderListItemResponse>());
        PurchasesApi purchases = new(Api);

        await purchases.GetPagedAsync(new PurchaseOrderQuery
        {
            Search = "  Quijote ",
            Status = "EMITIDA",
            SupplierId = 7,
            From = new DateOnly(2026, 10, 1),
            Page = 2,
            PageSize = 10
        });

        Assert.Equal("/api/compras?search=Quijote&status=EMITIDA&supplierId=7&from=2026-10-01&page=2&pageSize=10", Handler.LastPathAndQuery);
    }

    [Fact]
    public async Task Kardex_EnviaLibroAlmacenYFechas()
    {
        Handler.Respond = _ => Json(new KardexResponse());
        InventoryApi inventory = new(Api);

        await inventory.GetKardexAsync(new KardexQuery { BookId = 5, WarehouseId = 2, To = new DateOnly(2026, 12, 31) });

        Assert.Equal("/api/inventario/kardex?bookId=5&warehouseId=2&to=2026-12-31", Handler.LastPathAndQuery);
    }

    [Fact]
    public async Task Recepcion_SeEnviaAlEndpointDeRecepciones()
    {
        Handler.Respond = _ => Json(new ReceptionResponse { Id = 1, Number = "REC-000001" });
        ReceptionsApi receptions = new(Api);

        ApiResult<ReceptionResponse> result = await receptions.RegisterAsync(new RegisterReceptionRequest { PurchaseOrderId = 3 });

        Assert.True(result.Succeeded);
        Assert.Equal("/api/recepciones", Handler.LastPath);
    }

    [Fact]
    public async Task BuscarLibros_ConPermisoDeCatalogo_UsaElCatalogo()
    {
        await Session.SignInAsync(ValidLogin("Catalogo.Ver", "Inventario.Ver"));
        Handler.Respond = _ => Json(new PagedResponse<ProductListItemResponse>
        {
            Items = [new ProductListItemResponse { Id = 9, Code = "LIB-000009", Title = "Rayuela", Isbn13 = "9789992200019" }]
        });

        BookLookup lookup = new(new ProductsApi(Api, Options.Create(new ApiOptions())), new InventoryApi(Api), Session);
        BookOption book = Assert.Single(await lookup.SearchAsync("rayuela", CancellationToken.None));

        Assert.Equal("/api/productos", Handler.LastPath);
        Assert.Equal(new BookOption(9, "LIB-000009", "Rayuela", "9789992200019"), book);
    }

    [Fact]
    public async Task BuscarLibros_SinPermisoDeCatalogo_UsaLasExistencias()
    {
        await Session.SignInAsync(ValidLogin("Inventario.Ver"));
        Handler.Respond = _ => Json(new PagedResponse<StockItemResponse>
        {
            Items =
            [
                new StockItemResponse { BookId = 4, BookCode = "LIB-000004", Title = "Ficciones", WarehouseId = 1 },
                new StockItemResponse { BookId = 4, BookCode = "LIB-000004", Title = "Ficciones", WarehouseId = 2 }
            ]
        });

        BookLookup lookup = new(new ProductsApi(Api, Options.Create(new ApiOptions())), new InventoryApi(Api), Session);
        IEnumerable<BookOption> books = await lookup.SearchAsync("ficciones", CancellationToken.None);

        Assert.Equal("/api/inventario/existencias", Handler.LastPath);
        Assert.Single(books); // un libro aunque esté en dos almacenes
    }
}
