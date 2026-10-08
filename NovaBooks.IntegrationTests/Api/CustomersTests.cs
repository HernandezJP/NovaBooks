using System.Net;
using System.Net.Http.Json;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Customers;
using NovaBooks.IntegrationTests.Infrastructure;

namespace NovaBooks.IntegrationTests.Api;

public sealed class CustomersTests : ApiTestBase
{
    public CustomersTests(NovaBooksApiFactory factory)
        : base(factory)
    {
    }

    private async Task<CustomerCatalogsResponse> CatalogsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<CustomerCatalogsResponse>("api/clientes/catalogos"))!;

    private static SaveCustomerRequest NaturalPerson(CustomerCatalogsResponse catalogs) => new()
    {
        PersonType = SaveCustomerRequest.NaturalPerson,
        FirstName = "Ana",
        LastName = Unique("Prueba"),
        Nit = RandomDigits(7) + "K",
        Email = $"{Unique("cliente")}@correo.test",
        PhoneTypeId = catalogs.PhoneTypes.First().Id,
        Phone = RandomDigits(8)
    };

    [Fact]
    public async Task Crear_AsignaCodigoAutomaticoYNormalizaContactos()
    {
        HttpClient admin = await AdminAsync();
        SaveCustomerRequest request = NaturalPerson(await CatalogsAsync(admin));
        request.Email = request.Email!.ToUpperInvariant();

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/clientes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CustomerResponse customer = (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
        Assert.Matches(@"^CLI-\d{6}$", customer.Code);
        Assert.Equal(request.Email.ToLowerInvariant(), customer.Email);
        Assert.Equal(request.Phone, customer.Phone);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public async Task Crear_NitDuplicado_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        CustomerCatalogsResponse catalogs = await CatalogsAsync(admin);
        SaveCustomerRequest first = NaturalPerson(catalogs);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/clientes", first)).StatusCode);

        SaveCustomerRequest second = NaturalPerson(catalogs);
        second.Nit = first.Nit;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/clientes", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("NIT", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Crear_DatosInvalidos_DevuelveTodosLosErroresALaVez()
    {
        HttpClient admin = await AdminAsync();
        SaveCustomerRequest request = new()
        {
            PersonType = SaveCustomerRequest.NaturalPerson,
            FirstName = "Ana",
            LastName = "Prueba",
            Nit = "abc",
            Phone = "12",
            Email = "no-es-correo"
        };

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/clientes", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Nit", out _));
        Assert.True(errors.TryGetProperty("Phone", out _));
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("PhoneTypeId", out _));
    }

    [Fact]
    public async Task Actualizar_ReemplazaContactosYPermiteVolverAlAnterior()
    {
        HttpClient admin = await AdminAsync();
        SaveCustomerRequest request = NaturalPerson(await CatalogsAsync(admin));
        CustomerResponse created = (await (await admin.PostAsJsonAsync("api/clientes", request)).Content.ReadFromJsonAsync<CustomerResponse>())!;
        string originalPhone = request.Phone!;

        request.Phone = RandomDigits(8);
        HttpResponseMessage changed = await admin.PutAsJsonAsync($"api/clientes/{created.Id}", request);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(request.Phone, (await changed.Content.ReadFromJsonAsync<CustomerResponse>())!.Phone);

        // El contacto anterior quedó inactivo y se reutiliza sin chocar con índices únicos.
        request.Phone = originalPhone;
        HttpResponseMessage restored = await admin.PutAsJsonAsync($"api/clientes/{created.Id}", request);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        CustomerResponse final = (await restored.Content.ReadFromJsonAsync<CustomerResponse>())!;
        Assert.Equal(originalPhone, final.Phone);
        Assert.Equal(created.Code, final.Code);
    }

    [Fact]
    public async Task Actualizar_CambioDeTipoDePersona_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        SaveCustomerRequest request = NaturalPerson(await CatalogsAsync(admin));
        CustomerResponse created = (await (await admin.PostAsJsonAsync("api/clientes", request)).Content.ReadFromJsonAsync<CustomerResponse>())!;

        request.PersonType = SaveCustomerRequest.LegalPerson;
        request.LegalName = "Empresa de Prueba S.A.";

        HttpResponseMessage response = await admin.PutAsJsonAsync($"api/clientes/{created.Id}", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_EsLogicoYElClienteSigueConsultable()
    {
        HttpClient admin = await AdminAsync();
        SaveCustomerRequest request = NaturalPerson(await CatalogsAsync(admin));
        CustomerResponse created = (await (await admin.PostAsJsonAsync("api/clientes", request)).Content.ReadFromJsonAsync<CustomerResponse>())!;

        HttpResponseMessage delete = await admin.DeleteAsync($"api/clientes/{created.Id}");
        Assert.True(delete.IsSuccessStatusCode);

        CustomerResponse? stored = await admin.GetFromJsonAsync<CustomerResponse>($"api/clientes/{created.Id}");
        Assert.False(stored!.IsActive);

        PagedResponse<CustomerListItemResponse>? active =
            await admin.GetFromJsonAsync<PagedResponse<CustomerListItemResponse>>($"api/clientes?search={created.Code}&isActive=true");
        Assert.Empty(active!.Items);
    }

    [Fact]
    public async Task UsuarioSoloLectura_NoPuedeCrear()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("api/clientes", NaturalPerson(await CatalogsAsync(admin)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
