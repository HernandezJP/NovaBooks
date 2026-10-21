using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Pedidos de clientes. Confirmar valida la existencia libre y reserva, o
/// deja el pedido por aprobar si tiene un descuento mayor al permitido o
/// excede el crédito del cliente. Aprobar o rechazar requiere
/// Pedidos.Aprobar y no puede hacerlo quien registró el pedido.
/// </summary>
[AuditEntity("PedidoCliente")]
[ApiController]
[Route("api/pedidos")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    private SalesPermissionSet Permissions => new(
        this.HasPermission(SalesPermissions.ApplyDiscount),
        this.HasPermission(SalesPermissions.SellOnCredit),
        this.HasPermission(OrderPermissions.Approve));

    [HasAnyPermission(OrderPermissions.View, OrderPermissions.Create)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(OrderCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _orders.GetCatalogsAsync(cancellationToken));
    }

    /// <summary>Libros a la venta con su precio en la lista y la existencia libre en el almacén.</summary>
    [HasAnyPermission(OrderPermissions.Create, OrderPermissions.Update, SalesPermissions.Create)]
    [HttpGet("libros")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SalesBookOptionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SalesBookOptionResponse>>> SearchBooks(
        [FromQuery] string? search,
        [FromQuery] int? priceListId,
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        return Ok(await _orders.SearchBooksAsync(search, priceListId, warehouseId, cancellationToken));
    }

    [HasPermission(OrderPermissions.View)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrderListItemResponse>>> GetPaged(
        [FromQuery] OrderQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _orders.GetPagedAsync(query, cancellationToken));
    }

    [HasPermission(OrderPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        OrderResponse? order = await _orders.GetByIdAsync(id, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return order is null ? this.NotFoundProblem("El pedido no existe.") : Ok(order);
    }

    [HasPermission(OrderPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] SaveOrderRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<OrderResponse> result =
            await _orders.CreateAsync(request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(OrderPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Update(
        int id,
        [FromBody] SaveOrderRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<OrderResponse> result =
            await _orders.UpdateAsync(id, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasAnyPermission(OrderPermissions.Create, OrderPermissions.Update)]
    [HttpPost("{id:int}/confirmar")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Confirm(int id, CancellationToken cancellationToken)
    {
        OperationResult<OrderResponse> result =
            await _orders.ConfirmAsync(id, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(OrderPermissions.Approve)]
    [HttpPost("{id:int}/aprobar")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Approve(
        int id,
        [FromBody] ApproveOrderRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<OrderResponse> result =
            await _orders.ApproveAsync(id, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(OrderPermissions.Approve)]
    [HttpPost("{id:int}/rechazar")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Reject(
        int id,
        [FromBody] OrderReasonRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<OrderResponse> result =
            await _orders.RejectAsync(id, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(OrderPermissions.Cancel)]
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Cancel(
        int id,
        [FromBody] OrderReasonRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<OrderResponse> result =
            await _orders.CancelAsync(id, request, this.GetAuthenticatedUserId(), Permissions, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
