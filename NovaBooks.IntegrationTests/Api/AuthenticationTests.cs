using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NovaBooks.Application.Authentication;
using NovaBooks.Application.DTOs.Users;
using NovaBooks.IntegrationTests.Infrastructure;

namespace NovaBooks.IntegrationTests.Api;

public sealed class AuthenticationTests : ApiTestBase
{
    public AuthenticationTests(NovaBooksApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_CredencialesCorrectas_DevuelveTokenRolesYPermisos()
    {
        HttpResponseMessage response = await PostLoginAsync(Anonymous(), NovaBooksApiFactory.AdminUser, NovaBooksApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));
        Assert.Contains("Administrador", login.User.Roles);
        Assert.Contains("Usuarios.Ver", login.User.Permissions);
        Assert.True(login.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ConCorreo_TambienFunciona()
    {
        HttpResponseMessage response = await PostLoginAsync(Anonymous(), "admin@novabooks.test", NovaBooksApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ContrasenaIncorrecta_Devuelve401ConMensajeDeCredenciales()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Clientes.Ver");

        HttpResponseMessage response = await PostLoginAsync(Anonymous(), userName, "Incorrecta1*");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_credentials", await ReadProblemCodeAsync(response));
        Assert.Equal("El usuario o la contraseña son incorrectos.", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Login_UsuarioInexistente_DevuelveElMismoMensajeQueContrasenaIncorrecta()
    {
        HttpResponseMessage response = await PostLoginAsync(Anonymous(), "no-existe", "Cualquiera1*");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_credentials", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Login_CincoIntentosFallidos_BloqueaLaCuentaTemporalmente()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = Anonymous();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await PostLoginAsync(client, userName, "Incorrecta1*");
        }

        // Ni siquiera con la contraseña correcta mientras dure el bloqueo.
        HttpResponseMessage response = await PostLoginAsync(client, userName, TestPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("account_locked", await ReadProblemCodeAsync(response));
        Assert.Contains("bloqueada temporalmente", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Login_UsuarioDesactivado_InformaCuentaDesactivadaSoloConContrasenaCorrecta()
    {
        (string userName, int userId) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient admin = await AdminAsync();

        HttpResponseMessage disable = await admin.PatchAsJsonAsync($"api/usuarios/{userId}/estado", new ChangeUserStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        HttpResponseMessage correct = await PostLoginAsync(Anonymous(), userName, TestPassword);
        Assert.Equal("account_disabled", await ReadProblemCodeAsync(correct));

        // Con contraseña incorrecta no se revela que la cuenta existe y está desactivada.
        HttpResponseMessage wrong = await PostLoginAsync(Anonymous(), userName, "Incorrecta1*");
        Assert.Equal("invalid_credentials", await ReadProblemCodeAsync(wrong));
    }

    [Fact]
    public async Task Login_DatosIncompletos_Devuelve400()
    {
        HttpResponseMessage response = await PostLoginAsync(Anonymous(), "", "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_SinToken_Devuelve401()
    {
        HttpResponseMessage response = await Anonymous().GetAsync("api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_TokenInvalido_Devuelve401()
    {
        HttpClient client = Anonymous();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "no.es.un.token");

        HttpResponseMessage response = await client.GetAsync("api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ConToken_DevuelveUsuarioYPermisosVigentes()
    {
        HttpClient admin = await AdminAsync();

        AuthenticatedUserResponse? me = await admin.GetFromJsonAsync<AuthenticatedUserResponse>("api/auth/me");

        Assert.Equal(NovaBooksApiFactory.AdminUser, me!.UserName);
        Assert.Contains("Roles.AsignarPermisos", me.Permissions);
    }
}
