using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Web.Services.Api;

namespace TikalLibros.Web.Services.Session;

/// <summary>
/// Alertas del usuario (/api/alertas) para la campana del navbar. Se recargan
/// al cambiar la sesión, al navegar (como máximo una vez por minuto) y cada
/// pocos minutos desde el layout.
/// </summary>
public sealed class AlertState : IDisposable
{
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1);

    private readonly AlertsApi _alertsApi;
    private readonly SessionState _session;
    private DateTime _loadedAt = DateTime.MinValue;
    private bool _loading;

    public AlertState(AlertsApi alertsApi, SessionState session)
    {
        _alertsApi = alertsApi;
        _session = session;
        _session.Changed += OnSessionChanged;
    }

    public event Action? Changed;

    public AlertsResponse? Current { get; private set; }

    public string? ErrorMessage { get; private set; }

    public int Total => Current?.Total ?? 0;

    public async Task RefreshAsync(bool force = false)
    {
        if (!_session.IsAuthenticated)
        {
            Current = null;
            Changed?.Invoke();
            return;
        }

        if (_loading || (!force && DateTime.UtcNow - _loadedAt < MinimumInterval))
        {
            return;
        }

        _loading = true;

        try
        {
            ApiResult<AlertsResponse> result = await _alertsApi.GetAsync();
            ErrorMessage = result.Succeeded ? null : result.Message;

            if (result.Succeeded)
            {
                Current = result.Data;
                _loadedAt = DateTime.UtcNow;
            }
        }
        finally
        {
            _loading = false;
        }

        Changed?.Invoke();
    }

    public void Dispose()
    {
        _session.Changed -= OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        Current = null;
        _loadedAt = DateTime.MinValue;
        _ = RefreshAsync(force: true);
    }
}
