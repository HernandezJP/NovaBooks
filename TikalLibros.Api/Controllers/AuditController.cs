using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Audit;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>Consulta de la auditoría y del historial de accesos (solo lectura).</summary>
[ApiController]
[Route("api/auditoria")]
[HasPermission(ReportPermissions.Audit)]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(AuditCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _audit.GetCatalogsAsync(cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AuditEntryResponse>>> GetPaged(
        [FromQuery] AuditQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _audit.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AuditDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditDetailResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        AuditDetailResponse? entry = await _audit.GetByIdAsync(id, cancellationToken);

        return entry is null ? this.NotFoundProblem("El registro de auditoría no existe.") : Ok(entry);
    }

    [HttpGet("accesos")]
    [ProducesResponseType(typeof(PagedResponse<AccessLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AccessLogResponse>>> GetAccessLog(
        [FromQuery] AccessLogQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _audit.GetAccessLogAsync(query, cancellationToken));
    }
}
