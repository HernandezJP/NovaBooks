using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Products;
using NovaBooks.IntegrationTests.Infrastructure;

namespace NovaBooks.IntegrationTests.Api;

public sealed class ProductsTests : ApiTestBase
{
    // PNG mínimo de 1x1 píxel.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public ProductsTests(NovaBooksApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<ProductCatalogsResponse> CatalogsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ProductCatalogsResponse>("api/productos/catalogos"))!;

    private static SaveProductRequest NewBook(ProductCatalogsResponse catalogs, decimal? price = null) => new()
    {
        Title = Unique("Libro de prueba "),
        Isbn13 = NewIsbn13(),
        LanguageId = catalogs.Languages.First().Id,
        FormatId = catalogs.Formats.First().Id,
        TaxId = catalogs.Taxes.First().Id,
        AllowsSale = true,
        Prices = price is null ? null : [new ProductPriceRequest { PriceListId = catalogs.PriceLists.First().Id, Price = price.Value }],
        ReferenceCost = price is null ? null : price / 2
    };

    private static async Task<ProductResponse> CreateAsync(HttpClient client, SaveProductRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("api/productos", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    [Fact]
    public async Task Crear_AsignaCodigoYGuardaPrecioYCosto()
    {
        HttpClient admin = await AdminAsync();

        ProductResponse product = await CreateAsync(admin, NewBook(await CatalogsAsync(admin), 150m));

        Assert.Matches(@"^LIB-\d{6}$", product.Code);
        Assert.Equal(150m, Assert.Single(product.Prices).Price);
        Assert.Equal(75m, product.ReferenceCost);
    }

    [Fact]
    public async Task Crear_IsbnInvalido_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        SaveProductRequest request = NewBook(await CatalogsAsync(admin));
        request.Isbn13 = "9780000000000";

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/productos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("Isbn13", out _));
    }

    [Fact]
    public async Task Crear_IsbnDuplicado_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        ProductCatalogsResponse catalogs = await CatalogsAsync(admin);
        SaveProductRequest first = NewBook(catalogs);
        await CreateAsync(admin, first);

        SaveProductRequest second = NewBook(catalogs);
        second.Isbn13 = first.Isbn13;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/productos", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("ISBN-13", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task CambiarPrecio_CierraElAnteriorEnElHistorial()
    {
        HttpClient admin = await AdminAsync();
        SaveProductRequest request = NewBook(await CatalogsAsync(admin), 100m);
        ProductResponse product = await CreateAsync(admin, request);

        request.Prices![0].Price = 120m;
        HttpResponseMessage update = await admin.PutAsJsonAsync($"api/productos/{product.Id}", request);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        ProductResponse updated = (await update.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(120m, Assert.Single(updated.Prices).Price);
        ProductPriceResponse previous = Assert.Single(updated.PriceHistory, price => price.Price == 100m);
        Assert.NotNull(previous.EndDate);
    }

    [Fact]
    public async Task SinPermisoDePrecios_NoVeCostoNiPuedeCambiarPrecios()
    {
        HttpClient admin = await AdminAsync();
        ProductCatalogsResponse catalogs = await CatalogsAsync(admin);
        ProductResponse product = await CreateAsync(admin, NewBook(catalogs, 90m));

        (string userName, _) = await CreateUserWithPermissionsAsync("Catalogo.Ver", "Catalogo.Crear", "Catalogo.Modificar");
        HttpClient client = await LoginAsync(userName, TestPassword);

        ProductResponse? visible = await client.GetFromJsonAsync<ProductResponse>($"api/productos/{product.Id}");
        Assert.Null(visible!.ReferenceCost);

        PagedResponse<ProductListItemResponse>? list =
            await client.GetFromJsonAsync<PagedResponse<ProductListItemResponse>>($"api/productos?search={product.Code}");
        Assert.All(list!.Items, item => Assert.Null(item.ReferenceCost));

        HttpResponseMessage pricing = await client.PostAsJsonAsync("api/productos", NewBook(catalogs, 50m));
        Assert.Equal(HttpStatusCode.Forbidden, pricing.StatusCode);

        // Sin enviar precios sí puede crear.
        await CreateAsync(client, NewBook(catalogs));
    }

    [Fact]
    public async Task Imagen_SubirConsultarYEliminar()
    {
        HttpClient admin = await AdminAsync();
        ProductResponse product = await CreateAsync(admin, NewBook(await CatalogsAsync(admin)));

        using MultipartFormDataContent form = new();
        ByteArrayContent file = new(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "portada.png");

        HttpResponseMessage upload = await admin.PostAsync($"api/productos/{product.Id}/imagen", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.NotNull((await upload.Content.ReadFromJsonAsync<ProductResponse>())!.ImageVersion);

        // La portada es pública (la usan las etiquetas <img>).
        HttpResponseMessage image = await Anonymous().GetAsync($"api/productos/{product.Id}/imagen");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/productos/{product.Id}/imagen")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous().GetAsync($"api/productos/{product.Id}/imagen")).StatusCode);
    }

    [Fact]
    public async Task Imagen_ArchivoQueNoEsImagen_SeRechaza()
    {
        HttpClient admin = await AdminAsync();
        ProductResponse product = await CreateAsync(admin, NewBook(await CatalogsAsync(admin)));

        using MultipartFormDataContent form = new();
        ByteArrayContent file = new("esto no es una imagen"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "falsa.png");

        HttpResponseMessage upload = await admin.PostAsync($"api/productos/{product.Id}/imagen", form);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }
}
