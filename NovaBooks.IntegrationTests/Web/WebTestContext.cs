extern alias web;

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using NovaBooks.Application.Authentication;
using web::NovaBooks.Web.Services.Api;
using web::NovaBooks.Web.Services.Session;

namespace NovaBooks.IntegrationTests.Web;

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

        Session = new SessionState(new ProtectedSessionStorage(JSInterop.JSRuntime, new EphemeralDataProtectionProvider()));
        Navigation = Services.GetRequiredService<NavigationManager>();
        Api = new ApiClient(
            new HttpClient(Handler) { BaseAddress = new Uri("https://api.test/") },
            Session,
            Navigation,
            NullLogger<ApiClient>.Instance);
    }

    protected StubHttpHandler Handler { get; } = new();

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
            Email = "usuario@novabooks.test",
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

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastAuthorization = request.Headers.Authorization?.ToString();
        LastPath = request.RequestUri?.AbsolutePath;
        return Task.FromResult(Respond(request));
    }
}
