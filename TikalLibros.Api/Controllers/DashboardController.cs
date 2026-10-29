using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Indicadores del inicio. Cualquier usuario autenticado lo consulta; cada
/// sección solo se incluye si tiene el permiso que la respalda.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        HashSet<string> permissions = User.FindAll(CustomClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Ok(await _dashboard.GetAsync(permissions, cancellationToken));
    }
}
