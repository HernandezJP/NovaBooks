using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Órdenes de compra: borrador → emitida → recibida (parcial o total) o
/// cancelada. La recepción se registra en /api/recepciones.
/// </summary>
[ApiController]
[Route("api/compras")]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _orders;

    public PurchaseOrdersController(IPurchaseOrderService orders)
    {
        _orders = orders;
    }

    [HasPermission(PurchasingPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PurchaseOrderListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PurchaseOrderListItemResponse>>> GetPaged(
        [FromQuery] PurchaseOrderQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _orders.GetPagedAsync(query, cancellationToken));
    }

    [HasPermission(PurchasingPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        PurchaseOrderResponse? order = await _orders.GetByIdAsync(id, cancellationToken);

        return order is null
            ? this.NotFoundProblem("La orden de compra no existe.")
            : Ok(order);
    }

    [HasAnyPermission(PurchasingPermissions.View, PurchasingPermissions.Create, PurchasingPermissions.Update)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(PurchaseOrderCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PurchaseOrderCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _orders.GetCatalogsAsync(cancellationToken));
    }

    [HasAnyPermission(PurchasingPermissions.Create, PurchasingPermissions.Update)]
    [HttpGet("libros")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PurchaseBookOptionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PurchaseBookOptionResponse>>> SearchBooks(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await _orders.SearchBooksAsync(search, cancellationToken));
    }

    [HasPermission(PurchasingPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Create(
        [FromBody] SavePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<PurchaseOrderResponse> result =
            await _orders.CreateAsync(request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(PurchasingPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Update(
        int id,
        [FromBody] SavePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<PurchaseOrderResponse> result =
            await _orders.UpdateAsync(id, request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(PurchasingPermissions.Approve)]
    [HttpPost("{id:int}/emitir")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Issue(int id, CancellationToken cancellationToken)
    {
        OperationResult<PurchaseOrderResponse> result =
            await _orders.IssueAsync(id, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(PurchasingPermissions.Cancel)]
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderResponse>> Cancel(
        int id,
        [FromBody] CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<PurchaseOrderResponse> result =
            await _orders.CancelAsync(id, request, this.GetAuthenticatedUserId(), cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
