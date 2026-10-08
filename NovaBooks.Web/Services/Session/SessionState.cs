using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using NovaBooks.Application.Authentication;

namespace NovaBooks.Web.Services.Session;

/// <summary>
/// Sesión del usuario en el circuito actual. El LoginResponse (token y
/// datos del usuario, nunca la contraseña) se guarda cifrado en
/// sessionStorage mediante ProtectedSessionStorage.
/// </summary>
public sealed class SessionState
{
    private const string StorageKey = "novabooks.session";

    private readonly ProtectedSessionStorage _storage;

    private LoginResponse? _current;

    public SessionState(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public event Action? Changed;

    public bool IsInitialized { get; private set; }

    public bool IsAuthenticated =>
        _current is not null &&
        _current.ExpiresAtUtc > DateTime.UtcNow;

    public string? AccessToken =>
        IsAuthenticated ? _current!.AccessToken : null;

    public AuthenticatedUserResponse? User =>
        IsAuthenticated ? _current!.User : null;

    public bool HasPermission(string permission)
    {
        return User?.Permissions.Contains(
            permission,
            StringComparer.OrdinalIgnoreCase) == true;
    }

    public bool HasAnyPermission(params string[] permissions)
    {
        return permissions.Any(HasPermission);
    }

    /// <summary>
    /// Carga la sesión guardada. Requiere un circuito interactivo.
    /// </summary>
    public async Task LoadAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            ProtectedBrowserStorageResult<LoginResponse> result =
                await _storage.GetAsync<LoginResponse>(StorageKey);

            _current = result.Success ? result.Value : null;
        }
        catch (CryptographicException)
        {
            // Datos protegidos con otra clave (por ejemplo, tras reiniciar
            // la Web con otras claves): se descartan.
            _current = null;
            await TryDeleteAsync();
        }

        if (_current is not null && !IsAuthenticated)
        {
            _current = null;
            await TryDeleteAsync();
        }

        IsInitialized = true;
        Changed?.Invoke();
    }

    public async Task SignInAsync(LoginResponse response)
    {
        _current = response;
        IsInitialized = true;

        await _storage.SetAsync(StorageKey, response);

        Changed?.Invoke();
    }

    /// <summary>
    /// Actualiza roles y permisos vigentes (por ejemplo, desde /api/auth/me).
    /// </summary>
    public async Task UpdateUserAsync(AuthenticatedUserResponse user)
    {
        if (_current is null)
        {
            return;
        }

        _current.User = user;

        await _storage.SetAsync(StorageKey, _current);

        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        bool hadSession = _current is not null;

        _current = null;
        IsInitialized = true;

        await TryDeleteAsync();

        if (hadSession)
        {
            Changed?.Invoke();
        }
    }

    private async Task TryDeleteAsync()
    {
        try
        {
            await _storage.DeleteAsync(StorageKey);
        }
        catch (JSDisconnectedException)
        {
            // El navegador ya cerró el circuito; no hay nada que borrar.
        }
    }
}
