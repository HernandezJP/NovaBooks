using NovaBooks.Application.Authentication;
using NovaBooks.Web.Services.Api;

namespace NovaBooks.Web.Services.Session;

public sealed record LoginOutcome(bool Succeeded, string? ErrorMessage);

/// <summary>
/// Inicio, restauración y cierre de sesión contra NovaBooks.Api.
/// </summary>
public sealed class AuthService
{
    public const string UnexpectedLoginMessage =
        "Ocurrió un error inesperado al iniciar sesión.";

    private readonly ApiClient _api;
    private readonly SessionState _session;

    private Task? _restoreTask;

    public AuthService(ApiClient api, SessionState session)
    {
        _api = api;
        _session = session;
    }

    public async Task<LoginOutcome> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ApiResult<LoginResponse> result =
            await _api.PostAnonymousAsync<LoginResponse>(
                "api/auth/login",
                request,
                cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            await _session.SignInAsync(result.Data);
            return new LoginOutcome(true, null);
        }

        // Solo un 401 de la API indica credenciales, bloqueo o cuenta
        // desactivada; nunca se informa "contraseña incorrecta" por
        // errores de red, tiempo de espera o fallas del servidor.
        string message = result.Failure switch
        {
            ApiFailure.Unauthorized or ApiFailure.Validation => result.Message,
            ApiFailure.Network => ApiClient.NetworkMessage,
            ApiFailure.Timeout => ApiClient.TimeoutMessage,
            ApiFailure.SystemStarting => ApiClient.StartingMessage,
            _ => UnexpectedLoginMessage
        };

        return new LoginOutcome(false, message);
    }

    /// <summary>
    /// Carga la sesión guardada y la valida con /api/auth/me para obtener
    /// los permisos vigentes. Se ejecuta una vez por circuito.
    /// </summary>
    public Task RestoreAsync()
    {
        return _restoreTask ??= RestoreCoreAsync();
    }

    /// <summary>
    /// Vuelve a leer roles y permisos vigentes (por ejemplo, tras modificar
    /// los permisos del propio rol). Actualiza el menú si cambiaron.
    /// </summary>
    public async Task RefreshUserAsync()
    {
        ApiResult<AuthenticatedUserResponse> result =
            await _api.GetAsync<AuthenticatedUserResponse>("api/auth/me");

        if (result.Succeeded && result.Data is not null)
        {
            await _session.UpdateUserAsync(result.Data);
        }
    }

    public Task LogoutAsync()
    {
        return _session.SignOutAsync();
    }

    private async Task RestoreCoreAsync()
    {
        await _session.LoadAsync();

        if (!_session.IsAuthenticated)
        {
            return;
        }

        ApiResult<AuthenticatedUserResponse> result =
            await _api.GetAsync<AuthenticatedUserResponse>("api/auth/me");

        // Un 401 ya cerró la sesión en ApiClient. Si la API no responde,
        // se conserva la sesión; las siguientes llamadas mostrarán el error.
        if (result.Succeeded && result.Data is not null)
        {
            await _session.UpdateUserAsync(result.Data);
        }
    }
}
