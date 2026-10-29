using TikalLibros.Application.DTOs.Reports;

namespace TikalLibros.Application.Interfaces;

/// <summary>Alertas vigentes para el centro de alertas, según los permisos del usuario.</summary>
public interface IAlertService
{
    Task<AlertsResponse> GetAsync(
        IReadOnlySet<string> permissions,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
