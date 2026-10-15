using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Existencias, niveles mínimo/máximo, alertas, movimientos (kardex) y
/// ajustes. Los costos solo se envían con Inventario.VerCostos.
/// </summary>
[ApiController]
[Route("api/inventario")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InventoryController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    private bool CanViewCosts => this.HasPermission(InventoryPermissions.ViewCosts);

    [HasAnyPermission(InventoryPermissions.View, InventoryPermissions.Adjust, InventoryPermissions.ViewMovements)]
    [HttpGet("catalogos")]
    [ProducesResponseType(typeof(InventoryCatalogsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken)
    {
        return Ok(await _inventory.GetCatalogsAsync(cancellationToken));
    }

    [HasPermission(InventoryPermissions.View)]
    [HttpGet("existencias")]
    [ProducesResponseType(typeof(PagedResponse<StockItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<StockItemResponse>>> GetStock(
        [FromQuery] StockQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _inventory.GetStockAsync(query, CanViewCosts, cancellationToken));
    }

    [HasPermission(InventoryPermissions.Adjust)]
    [HttpPut("existencias/niveles")]
    [ProducesResponseType(typeof(StockItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockItemResponse>> SaveLevels(
        [FromBody] SaveStockLevelsRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<StockItemResponse> result = await _inventory.SaveLevelsAsync(request, CanViewCosts, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasPermission(InventoryPermissions.View)]
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StockAlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<StockAlertResponse>>> GetAlerts(
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        return Ok(await _inventory.GetAlertsAsync(warehouseId, cancellationToken));
    }

    [HasAnyPermission(InventoryPermissions.View, PurchasingPermissions.View)]
    [HttpGet("alertas/resumen")]
    [ProducesResponseType(typeof(StockAlertSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockAlertSummaryResponse>> GetAlertSummary(CancellationToken cancellationToken)
    {
        return Ok(await _inventory.GetAlertSummaryAsync(cancellationToken));
    }

    [HasPermission(InventoryPermissions.ViewMovements)]
    [HttpGet("movimientos")]
    [ProducesResponseType(typeof(PagedResponse<MovementListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MovementListItemResponse>>> GetMovements(
        [FromQuery] MovementQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _inventory.GetMovementsAsync(query, cancellationToken));
    }

    [HasPermission(InventoryPermissions.ViewMovements)]
    [HttpGet("movimientos/{id:int}")]
    [ProducesResponseType(typeof(MovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovementResponse>> GetMovement(int id, CancellationToken cancellationToken)
    {
        MovementResponse? movement = await _inventory.GetMovementAsync(id, CanViewCosts, cancellationToken);

        return movement is null
            ? this.NotFoundProblem("El movimiento de inventario no existe.")
            : Ok(movement);
    }

    [HasPermission(InventoryPermissions.ViewMovements)]
    [HttpGet("kardex")]
    [ProducesResponseType(typeof(KardexResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KardexResponse>> GetKardex(
        [FromQuery] KardexQuery query,
        CancellationToken cancellationToken)
    {
        OperationResult<KardexResponse> result = await _inventory.GetKardexAsync(query, CanViewCosts, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    [HasAnyPermission(InventoryPermissions.Adjust, InventoryPermissions.ViewMovements)]
    [HttpGet("ajustes")]
    [ProducesResponseType(typeof(PagedResponse<AdjustmentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdjustmentListItemResponse>>> GetAdjustments(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _inventory.GetAdjustmentsAsync(search, page, pageSize, cancellationToken));
    }

    [HasAnyPermission(InventoryPermissions.Adjust, InventoryPermissions.ViewMovements)]
    [HttpGet("ajustes/{id:int}")]
    [ProducesResponseType(typeof(AdjustmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdjustmentResponse>> GetAdjustment(int id, CancellationToken cancellationToken)
    {
        AdjustmentResponse? adjustment = await _inventory.GetAdjustmentAsync(id, CanViewCosts, cancellationToken);

        return adjustment is null
            ? this.NotFoundProblem("El ajuste de inventario no existe.")
            : Ok(adjustment);
    }

    [HasPermission(InventoryPermissions.Adjust)]
    [HttpPost("ajustes")]
    [ProducesResponseType(typeof(AdjustmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdjustmentResponse>> CreateAdjustment(
        [FromBody] SaveAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<AdjustmentResponse> result =
            await _inventory.CreateAdjustmentAsync(request, this.GetAuthenticatedUserId(), CanViewCosts, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetAdjustment), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }
}
