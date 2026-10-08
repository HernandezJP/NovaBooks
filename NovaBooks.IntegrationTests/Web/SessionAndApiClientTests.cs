extern alias web;

using System.Net;
using System.Text;
using NovaBooks.Application.Authentication;
using web::NovaBooks.Web.Services.Api;
using web::NovaBooks.Web.Services.Session;

namespace NovaBooks.IntegrationTests.Web;

public sealed class SessionStateTests : WebTestContext
{
    [Fact]
    public async Task SignIn_ExponeTokenYPermisosSinDistinguirMayusculas()
    {
        await Session.SignInAsync(ValidLogin("Clientes.Ver"));

        Assert.True(Session.IsAuthenticated);
        Assert.Equal("token-de-prueba", Session.AccessToken);
        Assert.True(Session.HasPermission("clientes.ver"));
        Assert.False(Session.HasPermission("Usuarios.Ver"));
        Assert.True(Session.HasAnyPermission("Usuarios.Ver", "Clientes.Ver"));
    }

    [Fact]
    public async Task SesionExpirada_NoSeConsideraAutenticada()
    {
        LoginResponse expired = ValidLogin("Clientes.Ver");
        expired.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        await Session.SignInAsync(expired);

        Assert.False(Session.IsAuthenticated);
        Assert.Null(Session.AccessToken);
        Assert.False(Session.HasPermission("Clientes.Ver"));
    }

    [Fact]
    public async Task SignOut_LimpiaLaSesionYNotifica()
    {
        await Session.SignInAsync(ValidLogin());
        int notifications = 0;
        Session.Changed += () => notifications++;

        await Session.SignOutAsync();

        Assert.False(Session.IsAuthenticated);
        Assert.Equal(1, notifications);
    }
}

public sealed class ApiClientTests : WebTestContext
{
    private static HttpResponseMessage Problem(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/problem+json") };

    [Fact]
    public async Task ConSesion_EnviaElTokenBearer()
    {
        await Session.SignInAsync(ValidLogin());

        await Api.GetAsync<bool>("api/clientes");

        Assert.Equal("Bearer token-de-prueba", Handler.LastAuthorization);
    }

    [Fact]
    public async Task Respuesta401_CierraSesionYRedirigeAlLogin()
    {
        await Session.SignInAsync(ValidLogin());
        Navigation.NavigateTo("clientes");
        Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        ApiResult<bool> result = await Api.GetAsync<bool>("api/clientes");

        Assert.Equal(ApiFailure.Unauthorized, result.Failure);
        Assert.False(Session.IsAuthenticated);
        Assert.EndsWith("login?reason=expired&returnUrl=%2Fclientes", Navigation.Uri);
    }

    [Fact]
    public async Task LoginAnonimo401_NoRedirigeYConservaElMensajeDeLaApi()
    {
        Handler.Respond = _ => Problem(HttpStatusCode.Unauthorized,
            """{"detail":"El usuario o la contraseña son incorrectos.","code":"invalid_credentials"}""");
        string before = Navigation.Uri;

        ApiResult<LoginResponse> result = await Api.PostAnonymousAsync<LoginResponse>("api/auth/login", new { });

        Assert.Equal("El usuario o la contraseña son incorrectos.", result.Message);
        Assert.Equal("invalid_credentials", result.ErrorCode);
        Assert.Null(Handler.LastAuthorization);
        Assert.Equal(before, Navigation.Uri);
    }

    [Fact]
    public async Task ErrorDeRed_DevuelveMensajeDeConexion()
    {
        Handler.Respond = _ => throw new HttpRequestException("Connection refused");

        ApiResult<bool> result = await Api.GetAsync<bool>("api/clientes");

        Assert.Equal(ApiFailure.Network, result.Failure);
        Assert.Equal(ApiClient.NetworkMessage, result.Message);
    }

    [Fact]
    public async Task TiempoAgotado_DevuelveMensajeDeEspera()
    {
        Handler.Respond = _ => throw new TaskCanceledException();

        ApiResult<bool> result = await Api.GetAsync<bool>("api/clientes");

        Assert.Equal(ApiFailure.Timeout, result.Failure);
        Assert.Equal(ApiClient.TimeoutMessage, result.Message);
    }

    [Theory]
    [InlineData("""{"code":"system_starting"}""", ApiFailure.SystemStarting)]
    [InlineData("""{"code":"database_unavailable"}""", ApiFailure.Unavailable)]
    public async Task Respuesta503_DistingueArranqueDeIndisponibilidad(string json, ApiFailure expected)
    {
        Handler.Respond = _ => Problem(HttpStatusCode.ServiceUnavailable, json);

        ApiResult<bool> result = await Api.GetAsync<bool>("api/clientes");

        Assert.Equal(expected, result.Failure);
    }

    [Fact]
    public async Task Respuesta400_LeeLosErroresPorCampo()
    {
        Handler.Respond = _ => Problem(HttpStatusCode.BadRequest,
            """{"title":"Errores de validación","errors":{"Nit":["El NIT no es válido."],"Phone":["El teléfono no es válido."]}}""");

        ApiResult<bool> result = await Api.PostAsync<bool>("api/clientes", new { });

        Assert.Equal(ApiFailure.Validation, result.Failure);
        Assert.Equal(["El NIT no es válido."], result.ValidationErrors["Nit"]);
        Assert.True(result.ValidationErrors.ContainsKey("Phone"));
        Assert.Equal("El NIT no es válido.", result.Message);
    }

    [Fact]
    public async Task Respuesta500_NoMuestraDetallesInternos()
    {
        Handler.Respond = _ => Problem(HttpStatusCode.InternalServerError,
            """{"detail":"System.NullReferenceException at Servicio.Metodo()"}""");

        ApiResult<bool> result = await Api.GetAsync<bool>("api/clientes");

        Assert.Equal(ApiFailure.Server, result.Failure);
        Assert.DoesNotContain("Exception", result.Message);
    }
}

public sealed class AuthServiceTests : WebTestContext
{
    private AuthService Auth => new(Api, Session);

    private static LoginRequest Request => new() { UserNameOrEmail = "usuario", Password = "Clave123*" };

    [Fact]
    public async Task LoginCorrecto_IniciaSesion()
    {
        Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = System.Net.Http.Json.JsonContent.Create(ValidLogin("Clientes.Ver"))
        };

        LoginOutcome outcome = await Auth.LoginAsync(Request);

        Assert.True(outcome.Succeeded);
        Assert.True(Session.HasPermission("Clientes.Ver"));
    }

    [Fact]
    public async Task CuentaBloqueada_MuestraElMensajeDeLaApi()
    {
        Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"detail":"La cuenta está bloqueada temporalmente.","code":"account_locked"}""", Encoding.UTF8, "application/problem+json")
        };

        LoginOutcome outcome = await Auth.LoginAsync(Request);

        Assert.False(outcome.Succeeded);
        Assert.Equal("La cuenta está bloqueada temporalmente.", outcome.ErrorMessage);
        Assert.False(Session.IsAuthenticated);
    }

    [Fact]
    public async Task ApiCaida_NoDiceQueLaContrasenaEsIncorrecta()
    {
        Handler.Respond = _ => throw new HttpRequestException("Connection refused");

        LoginOutcome outcome = await Auth.LoginAsync(Request);

        Assert.Equal(ApiClient.NetworkMessage, outcome.ErrorMessage);
    }

    [Fact]
    public async Task ErrorDelServidor_MuestraMensajeGenerico()
    {
        Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        LoginOutcome outcome = await Auth.LoginAsync(Request);

        Assert.Equal(AuthService.UnexpectedLoginMessage, outcome.ErrorMessage);
    }

    [Fact]
    public async Task Logout_CierraLaSesion()
    {
        await Session.SignInAsync(ValidLogin());

        await Auth.LogoutAsync();

        Assert.False(Session.IsAuthenticated);
    }
}
