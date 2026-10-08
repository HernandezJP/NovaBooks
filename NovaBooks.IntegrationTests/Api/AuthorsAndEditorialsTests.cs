using System.Net;
using System.Net.Http.Json;
using NovaBooks.Application.DTOs.Authors;
using NovaBooks.Application.DTOs.Editorials;
using NovaBooks.IntegrationTests.Infrastructure;

namespace NovaBooks.IntegrationTests.Api;

public sealed class AuthorsAndEditorialsTests : ApiTestBase
{
    public AuthorsAndEditorialsTests(NovaBooksApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Autor_CrearDuplicadoYDesactivar()
    {
        HttpClient admin = await AdminAsync();
        SaveAuthorRequest request = new() { FirstName = "Miguel", LastName = Unique("Asturias") };

        HttpResponseMessage created = await admin.PostAsJsonAsync("api/autores", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        AuthorResponse author = (await created.Content.ReadFromJsonAsync<AuthorResponse>())!;

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/autores", request)).StatusCode);

        HttpResponseMessage disabled = await admin.PatchAsJsonAsync($"api/autores/{author.Id}/estado", new ChangeAuthorStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.False((await disabled.Content.ReadFromJsonAsync<AuthorResponse>())!.IsActive);
    }

    [Fact]
    public async Task Autor_FechaDeFallecimientoAnteriorANacimiento_Devuelve400()
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/autores", new SaveAuthorRequest
        {
            FirstName = "Autor",
            LastName = Unique("Fechas"),
            BirthDate = new DateOnly(1950, 1, 1),
            DeathDate = new DateOnly(1940, 1, 1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Editorial_CodigoAutomaticoYNombreDuplicado()
    {
        HttpClient admin = await AdminAsync();
        SaveEditorialRequest request = new() { Name = Unique("Editorial ") };

        HttpResponseMessage created = await admin.PostAsJsonAsync("api/editoriales", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Matches(@"^EDI-\d{4}$", (await created.Content.ReadFromJsonAsync<EditorialResponse>())!.Code);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/editoriales", request)).StatusCode);
    }

    [Fact]
    public async Task Editorial_SinPermisoDeCreacion_Devuelve403()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Catalogo.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/editoriales")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/editoriales", new SaveEditorialRequest { Name = Unique("Editorial ") })).StatusCode);
    }
}
