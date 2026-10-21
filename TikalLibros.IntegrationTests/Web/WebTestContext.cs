extern alias web;

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using TikalLibros.Application.Authentication;
using web::TikalLibros.Web.Services.Api;
using web::TikalLibros.Web.Services.Session;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>
/// Contexto bUnit con MudBlazor, JS en modo flexible y una API simulada
/// mediante <see cref="StubHttpHandler"/>.
/// </summary>
public abstract class WebTestContext : BunitContext
{
    protected WebTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        // Clientes de la API para renderizar páginas; se crean al pedirlos,
        // cuando la API simulada ya existe.
        Services.AddSingleton(_ => new SalesApi(Api));
        Services.AddSingleton(_ => new OrdersApi(Api));
        Services.AddSingleton(_ => new CashApi(Api));
        Services.AddSingleton(_ => new ReportsApi(Api));

        Session = new SessionState(new ProtectedSessionStorage(JSInterop.JSRuntime, new EphemeralDataProtectionProvider()));
        Navigation = Services.GetRequiredService<NavigationManager>();
        Api = new ApiClient(
            new HttpClient(Handler) { BaseAddress = new Uri("https://api.test/") },
            Session,
            Navigation,
            NullLogger<ApiClient>.Instance);
    }

    protected StubHttpHandler Handler { get; } = new();

    private bool _disposing;

    /// <summary>
    /// xUnit libera el contexto con Dispose(); algunos servicios de MudBlazor
    /// (por ejemplo, el de los selectores de fecha) solo admiten liberación
    /// asíncrona, así que se delega en DisposeAsync.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            return;
        }

        base.Dispose(disposing);
    }


    protected SessionState Session { get; }

    protected NavigationManager Navigation { get; }

    protected ApiClient Api { get; }

    protected static LoginResponse ValidLogin(params string[] permissions) => new()
    {
        AccessToken = "token-de-prueba",
        ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
        User = new AuthenticatedUserResponse
        {
            Id = "1",
            UserName = "usuario",
            Email = "usuario@tikallibros.test",
            Roles = ["Vendedor"],
            Permissions = permissions
        }
    };
}

/// <summary>Responde con la función configurada y recuerda la última solicitud.</summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
        _ => new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);

    public string? LastAuthorization { get; private set; }

    public string? LastPath { get; private set; }

    /// <summary>Ruta y consulta de la última solicitud ("/api/compras?status=EMITIDA").</summary>
    public string? LastPathAndQuery { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastAuthorization = request.Headers.Authorization?.ToString();
        LastPath = request.RequestUri?.AbsolutePath;
        LastPathAndQuery = request.RequestUri?.PathAndQuery;

        return Task.FromResult(Respond(request));
    }
}
