using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Common;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Centro de alertas. Cualquier usuario autenticado lo consulta; solo recibe
/// las alertas que puede atender con sus permisos.
/// </summary>
[ApiController]
[Route("api/alertas")]
[Authorize]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertService _alerts;

    public AlertsController(IAlertService alerts)
    {
        _alerts = alerts;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AlertsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertsResponse>> Get(CancellationToken cancellationToken)
    {
        HashSet<string> permissions = User.FindAll(CustomClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Ok(await _alerts.GetAsync(permissions, this.GetAuthenticatedUserId(), cancellationToken));
    }
}
