using TikalLibros.Application.DTOs.Reports;

namespace TikalLibros.Application.Interfaces;

/// <summary>Indicadores y series del inicio, según los permisos del usuario.</summary>
public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(IReadOnlySet<string> permissions, CancellationToken cancellationToken = default);
}
