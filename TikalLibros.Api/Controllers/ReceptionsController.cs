using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Recepciones de compra. Cada recepción actualiza el inventario en la
/// misma transacción (lotes, existencias y movimiento de entrada).
/// </summary>
[ApiController]
[Route("api/recepciones")]
public sealed class ReceptionsController : ControllerBase
{
    private readonly IReceptionService _receptions;

    public ReceptionsController(IReceptionService receptions)
    {
        _receptions = receptions;
    }

    [HasPermission(PurchasingPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ReceptionListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ReceptionListItemResponse>>> GetPaged(
        [FromQuery] ReceptionQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _receptions.GetPagedAsync(query, cancellationToken));
    }

    [HasPermission(PurchasingPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReceptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReceptionResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        ReceptionResponse? reception = await _receptions.GetByIdAsync(id, cancellationToken);

        return reception is null
            ? this.NotFoundProblem("La recepción no existe.")
            : Ok(reception);
    }

    [HasAnyPermission(PurchasingPermissions.View, PurchasingPermissions.Receive)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(ReceptionCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceptionCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _receptions.GetCatalogsAsync(cancellationToken));
    }

    [HasPermission(PurchasingPermissions.Receive)]
    [HttpPost]
    [ProducesResponseType(typeof(ReceptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReceptionResponse>> Register(
        [FromBody] RegisterReceptionRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<ReceptionResponse> result =
            await _receptions.RegisterAsync(request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    /// <summary>
    /// Registra y recibe una compra en un paso: requiere crear, aprobar y
    /// recibir compras.
    /// </summary>
    [HasPermission(PurchasingPermissions.Create)]
    [HasPermission(PurchasingPermissions.Approve)]
    [HasPermission(PurchasingPermissions.Receive)]
    [HttpPost("compra-directa")]
    [ProducesResponseType(typeof(ReceptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReceptionResponse>> RegisterDirectPurchase(
        [FromBody] DirectPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<ReceptionResponse> result =
            await _receptions.RegisterDirectPurchaseAsync(request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(PurchasingPermissions.Receive)]
    [HasPermission(PurchasingPermissions.Cancel)]
    [HttpPost("{id:int}/anular")]
    [ProducesResponseType(typeof(ReceptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReceptionResponse>> Annul(
        int id,
        [FromBody] AnnulReceptionRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<ReceptionResponse> result =
            await _receptions.AnnulAsync(id, request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
