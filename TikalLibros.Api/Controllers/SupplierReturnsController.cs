using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Devoluciones de mercadería al proveedor sobre una recepción. Registrar
/// requiere Compras.Devolver; anular, Compras.Cancelar.
/// </summary>
[AuditEntity("DevolucionProveedor")]
[ApiController]
[Route("api/devoluciones-proveedor")]
public sealed class SupplierReturnsController : ControllerBase
{
    private readonly ISupplierReturnService _returns;

    public SupplierReturnsController(ISupplierReturnService returns)
    {
        _returns = returns;
    }

    [HasAnyPermission(PurchasingPermissions.Return, PurchasingPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SupplierReturnListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SupplierReturnListItemResponse>>> GetPaged(
        [FromQuery] SupplierReturnQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _returns.GetPagedAsync(query, cancellationToken));

    [HasAnyPermission(PurchasingPermissions.Return, PurchasingPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierReturnResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        SupplierReturnResponse? entry = await _returns.GetByIdAsync(id, cancellationToken);

        return entry is null ? this.NotFoundProblem("La devolución al proveedor no existe.") : Ok(entry);
    }

    [HasPermission(PurchasingPermissions.Return)]
    [HttpGet("recepciones/{receptionId:int}")]
    [ProducesResponseType(typeof(ReturnableReceptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnableReceptionResponse>> GetReturnable(int receptionId, CancellationToken cancellationToken)
    {
        ReturnableReceptionResponse? reception = await _returns.GetReturnableAsync(receptionId, cancellationToken);

        return reception is null ? this.NotFoundProblem("La recepción no existe o está anulada.") : Ok(reception);
    }

    [HasPermission(PurchasingPermissions.Return)]
    [HttpPost]
    [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierReturnResponse>> Create(
        [FromBody] SaveSupplierReturnRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SupplierReturnResponse> result =
            await _returns.CreateAsync(request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(PurchasingPermissions.Cancel)]
    [HttpPost("{id:int}/anular")]
    [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierReturnResponse>> Annul(
        int id,
        [FromBody] AnnulSupplierReturnRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<SupplierReturnResponse> result =
            await _returns.AnnulAsync(id, request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
