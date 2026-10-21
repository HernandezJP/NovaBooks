using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Configuration;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Parámetros del sistema: descuento sin aprobación, descuento máximo y días
/// de reserva de los pedidos. Cualquier usuario autenticado puede leerlos
/// (las pantallas de venta los muestran); modificarlos requiere permiso.
/// </summary>
[AuditEntity("ParametroSistema")]
[ApiController]
[Route("api/parametros")]
public sealed class SystemParametersController : ControllerBase
{
    private readonly ISystemParameterService _parameters;

    public SystemParametersController(ISystemParameterService parameters)
    {
        _parameters = parameters;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SystemParameterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SystemParameterResponse>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _parameters.GetAllAsync(cancellationToken));
    }

    [HasPermission(ConfigurationPermissions.ManageParameters)]
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(SystemParameterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemParameterResponse>> Update(
        string key,
        [FromBody] UpdateSystemParameterRequest request,
        CancellationToken cancellationToken = default)
    {
        OperationResult<SystemParameterResponse> result = await _parameters.UpdateAsync(key, request, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
