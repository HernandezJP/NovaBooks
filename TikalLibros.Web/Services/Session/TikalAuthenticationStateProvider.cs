using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TikalLibros.Web.Security;

namespace TikalLibros.Web.Services.Session;

/// <summary>
/// Expone la sesión como ClaimsPrincipal para AuthorizeView y
/// CascadingAuthenticationState.
/// </summary>
public sealed class TikalAuthenticationStateProvider
    : AuthenticationStateProvider, IDisposable
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly SessionState _session;

    public TikalAuthenticationStateProvider(SessionState session)
    {
        _session = session;
        _session.Changed += OnSessionChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_session.User is not { } user)
        {
            return Task.FromResult(Anonymous);
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email)
        ];

        claims.AddRange(user.Roles.Select(role =>
            new Claim(ClaimTypes.Role, role)));

        claims.AddRange(user.Permissions.Select(permission =>
            new Claim(AppPermissions.ClaimType, permission)));

        ClaimsIdentity identity = new(claims, "Tikal Libros");

        return Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void Dispose()
    {
        _session.Changed -= OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
