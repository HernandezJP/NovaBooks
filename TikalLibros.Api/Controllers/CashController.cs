using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Caja: apertura, movimientos manuales y cierre con arqueo. Solo quien
/// abrió la caja registra movimientos en ella; anular movimientos de otros
/// o cerrar su caja requiere el permiso de supervisión Caja.AnularMovimiento.
/// </summary>
[AuditEntity("AperturaCaja")]
[ApiController]
[Route("api/caja")]
public sealed class CashController : ControllerBase
{
    private readonly ICashService _cash;

    public CashController(ICashService cash)
    {
        _cash = cash;
    }

    private CashPermissionSet Permissions => new(
        this.HasPermission(CashPermissions.CreateIncome),
        this.HasPermission(CashPermissions.CreateExpense),
        this.HasPermission(CashPermissions.CancelMovement));

    [HasAnyPermission(CashPermissions.View, CashPermissions.Open)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(CashCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CashCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _cash.GetCatalogsAsync(cancellationToken));
    }

    [HasPermission(CashPermissions.View)]
    [HttpGet("aperturas")]
    [ProducesResponseType(typeof(PagedResponse<CashSessionListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CashSessionListItemResponse>>> GetSessions(
        [FromQuery] CashSessionQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _cash.GetSessionsAsync(query, cancellationToken));
    }

    /// <summary>La caja abierta del usuario; 204 si no tiene ninguna.</summary>
    [HasAnyPermission(CashPermissions.View, CashPermissions.Open)]
    [HttpGet("aperturas/actual")]
    [ProducesResponseType(typeof(CashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CashSessionResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        CashSessionResponse? session = await _cash.GetCurrentSessionAsync(this.GetAuthenticatedUserId(), cancellationToken);

        return session is null ? NoContent() : Ok(session);
    }

    [HasPermission(CashPermissions.View)]
    [HttpGet("aperturas/{id:int}")]
    [ProducesResponseType(typeof(CashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashSessionResponse>> GetSession(int id, CancellationToken cancellationToken)
    {
        CashSessionResponse? session = await _cash.GetSessionAsync(id, this.GetAuthenticatedUserId(), cancellationToken);

        return session is null ? this.NotFoundProblem("La apertura de caja no existe.") : Ok(session);
    }

    [HasPermission(CashPermissions.Open)]
    [HttpPost("aperturas")]
    [ProducesResponseType(typeof(CashSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashSessionResponse>> Open(
        [FromBody] OpenCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<CashSessionResponse> result =
            await _cash.OpenAsync(request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetSession), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasAnyPermission(CashPermissions.CreateIncome, CashPermissions.CreateExpense)]
    [HttpPost("aperturas/{id:int}/movimientos")]
    [ProducesResponseType(typeof(CashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashSessionResponse>> RegisterMovement(
        int id,
        [FromBody] CashMovementRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<CashSessionResponse> result =
            await _cash.RegisterMovementAsync(id, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(CashPermissions.CancelMovement)]
    [HttpPost("movimientos/{id:int}/anular")]
    [ProducesResponseType(typeof(CashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashSessionResponse>> AnnulMovement(
        int id,
        [FromBody] AnnulCashMovementRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<CashSessionResponse> result =
            await _cash.AnnulMovementAsync(id, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(CashPermissions.Close)]
    [HttpPost("aperturas/{id:int}/cerrar")]
    [ProducesResponseType(typeof(CashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashSessionResponse>> Close(
        int id,
        [FromBody] CloseCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<CashSessionResponse> result =
            await _cash.CloseAsync(id, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
