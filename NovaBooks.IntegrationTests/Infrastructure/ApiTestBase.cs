using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NovaBooks.Application.Authentication;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.DTOs.Users;

namespace NovaBooks.IntegrationTests.Infrastructure;

/// <summary>
/// Utilidades comunes: inicio de sesión, usuarios de prueba con permisos
/// concretos y lectura de ProblemDetails.
/// </summary>
public abstract class ApiTestBase : IClassFixture<NovaBooksApiFactory>
{
    protected const string TestPassword = "Prueba123*";

    private static int _sequence;

    protected ApiTestBase(NovaBooksApiFactory factory)
    {
        Factory = factory;
    }

    protected NovaBooksApiFactory Factory { get; }

    protected static string Unique(string prefix) =>
        $"{prefix}{Interlocked.Increment(ref _sequence)}{Guid.NewGuid().ToString("N")[..6]}";

    protected static string RandomDigits(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(index =>
            (index == 0 ? Random.Shared.Next(1, 10) : Random.Shared.Next(0, 10)).ToString()));

    /// <summary>ISBN-13 aleatorio con dígito de control válido.</summary>
    protected static string NewIsbn13()
    {
        string body = "978" + RandomDigits(9);
        int sum = body.Select((digit, index) => (digit - '0') * (index % 2 == 0 ? 1 : 3)).Sum();
        return body + ((10 - sum % 10) % 10);
    }

    protected HttpClient Anonymous() => Factory.CreateClient();

    protected Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string user, string password) =>
        client.PostAsJsonAsync("api/auth/login", new LoginRequest { UserNameOrEmail = user, Password = password });

    protected async Task<HttpClient> LoginAsync(string user, string password)
    {
        HttpClient client = Factory.CreateClient();
        HttpResponseMessage response = await PostLoginAsync(client, user, password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        return client;
    }

    protected Task<HttpClient> AdminAsync() =>
        LoginAsync(NovaBooksApiFactory.AdminUser, NovaBooksApiFactory.AdminPassword);

    /// <summary>
    /// Crea (con el administrador) un rol con los permisos indicados y un
    /// usuario con ese rol. Devuelve su nombre de usuario e id.
    /// </summary>
    protected async Task<(string UserName, int UserId)> CreateUserWithPermissionsAsync(params string[] permissions)
    {
        HttpClient admin = await AdminAsync();

        string roleName = Unique("Rol");
        HttpResponseMessage roleResponse = await admin.PostAsJsonAsync("api/roles", new CreateRoleRequest { Name = roleName });
        Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
        RoleResponse role = (await roleResponse.Content.ReadFromJsonAsync<RoleResponse>())!;

        HttpResponseMessage permissionsResponse = await admin.PutAsJsonAsync(
            $"api/roles/{role.Id}/permisos",
            new AssignRolePermissionsRequest { Permissions = permissions.ToList() });
        Assert.Equal(HttpStatusCode.OK, permissionsResponse.StatusCode);

        string userName = Unique("usuario");
        HttpResponseMessage userResponse = await admin.PostAsJsonAsync("api/usuarios", new CreateUserRequest
        {
            UserName = userName,
            Email = $"{userName}@novabooks.test",
            Password = TestPassword,
            Roles = [roleName]
        });
        Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
        UserResponse user = (await userResponse.Content.ReadFromJsonAsync<UserResponse>())!;

        return (userName, user.Id);
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement.Clone();
    }

    /// <summary>Lee el campo "code" de un ProblemDetails.</summary>
    protected static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        JsonElement json = await ReadJsonAsync(response);
        return json.TryGetProperty("code", out JsonElement code) ? code.GetString() : null;
    }

    protected static async Task<string?> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        JsonElement json = await ReadJsonAsync(response);
        return json.TryGetProperty("detail", out JsonElement detail) ? detail.GetString() : null;
    }
}
