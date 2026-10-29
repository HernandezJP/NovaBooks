using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Audit;

namespace TikalLibros.Application.Interfaces;

/// <summary>Consulta de la auditoría y del historial de accesos (solo lectura).</summary>
public interface IAuditService
{
    Task<AuditCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResponse<AuditEntryResponse>> GetPagedAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default);

    Task<AuditDetailResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<AccessLogResponse>> GetAccessLogAsync(
        AccessLogQuery query,
        CancellationToken cancellationToken = default);
}
