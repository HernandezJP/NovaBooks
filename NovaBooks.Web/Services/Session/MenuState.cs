using MudBlazor;
using NovaBooks.Application.DTOs.Menu;
using NovaBooks.Web.Services.Api;

namespace NovaBooks.Web.Services.Session;

/// <summary>
/// Menú del usuario obtenido de /api/menu (filtrado por permisos vigentes).
/// Se recarga cuando cambia la sesión.
/// </summary>
public sealed class MenuState : IDisposable
{
    private readonly MenuApi _menuApi;
    private readonly SessionState _session;

    private Task? _loading;

    public MenuState(MenuApi menuApi, SessionState session)
    {
        _menuApi = menuApi;
        _session = session;
        _session.Changed += OnSessionChanged;
    }

    public event Action? Changed;

    public IReadOnlyList<MenuGroupResponse> Groups { get; private set; } = [];

    public bool IsLoading => _loading is { IsCompleted: false };

    public string? ErrorMessage { get; private set; }

    public Task EnsureLoadedAsync()
    {
        return _loading ??= LoadAsync();
    }

    public Task ReloadAsync()
    {
        _loading = LoadAsync();
        return _loading;
    }

    public static string IconFor(string iconKey)
    {
        return iconKey switch
        {
            "home" => Icons.Material.Outlined.SpaceDashboard,
            "users" => Icons.Material.Outlined.People,
            "roles" => Icons.Material.Outlined.AdminPanelSettings,
            "permissions" => Icons.Material.Outlined.Key,
            "books" => Icons.Material.Outlined.MenuBook,
            "authors" => Icons.Material.Outlined.HistoryEdu,
            "publishers" => Icons.Material.Outlined.AccountBalance,
            "customers" => Icons.Material.Outlined.Badge,
            _ => Icons.Material.Outlined.Circle
        };
    }

    public void Dispose()
    {
        _session.Changed -= OnSessionChanged;
    }

    private async Task LoadAsync()
    {
        _loadedFor = CurrentSignature();

        if (!_session.IsAuthenticated)
        {
            Groups = [];
            ErrorMessage = null;
            Changed?.Invoke();
            return;
        }

        Changed?.Invoke();

        ApiResult<List<MenuGroupResponse>> result = await _menuApi.GetAsync();

        if (result.Succeeded && result.Data is not null)
        {
            Groups = result.Data;
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = result.Message;
        }

        Changed?.Invoke();
    }

    private string? _loadedFor;

    private void OnSessionChanged()
    {
        // Solo recarga si cambió el usuario o sus permisos, para no repetir
        // solicitudes cuando la sesión se restaura y luego se valida.
        if (CurrentSignature() == _loadedFor)
        {
            return;
        }

        _ = ReloadAsync();
    }

    private string? CurrentSignature()
    {
        return _session.User is { } user
            ? user.Id + "|" + string.Join(",", user.Permissions.Order())
            : null;
    }
}
