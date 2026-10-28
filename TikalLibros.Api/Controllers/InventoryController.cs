using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.Infrastructure.Services.Inventory;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Existencias, niveles mínimo/máximo, alertas, movimientos (kardex),
/// ajustes y traslados. Los costos solo se envían con Inventario.VerCostos.
/// </summary>
[AuditEntity("Inventario")]
[ApiController]
[Route("api/inventario")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;
    private readonly ITransferService _transfers;

    public InventoryController(IInventoryService inventory, ITransferService transfers)
    {
        _inventory = inventory;
        _transfers = transfers;
    }

    private bool CanViewCosts => this.HasPermission(InventoryPermissions.ViewCosts);

    [HasAnyPermission(InventoryPermissions.View, InventoryPermissions.Adjust, InventoryPermissions.ViewMovements, InventoryPermissions.Transfer)]
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
    [AuditEntity("Existencia", "MODIFICAR_NIVELES")]
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
    [AuditEntity("AjusteInventario", "CREAR")]
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

    [HasAnyPermission(InventoryPermissions.Transfer, InventoryPermissions.ViewMovements)]
    [HttpGet("traslados")]
    [ProducesResponseType(typeof(PagedResponse<TransferListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TransferListItemResponse>>> GetTransfers(
        [FromQuery] TransferQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _transfers.GetPagedAsync(query, cancellationToken));
    }

    [HasAnyPermission(InventoryPermissions.Transfer, InventoryPermissions.ViewMovements)]
    [HttpGet("traslados/{id:int}")]
    [ProducesResponseType(typeof(MovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovementResponse>> GetTransfer(int id, CancellationToken cancellationToken)
    {
        MovementResponse? movement = await _inventory.GetMovementAsync(id, CanViewCosts, cancellationToken);

        return movement is null || movement.TypeCode != TransferService.TransferType
            ? this.NotFoundProblem("El traslado solicitado no existe.")
            : Ok(movement);
    }

    [HasPermission(InventoryPermissions.Transfer)]
    [HttpPost("traslados")]
    [AuditEntity("TrasladoInventario", "CREAR")]
    [ProducesResponseType(typeof(MovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> CreateTransfer(
        [FromBody] SaveTransferRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<MovementResponse> result =
            await _transfers.CreateAsync(request, this.GetAuthenticatedUserId(), CanViewCosts, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetTransfer), new { id = result.Data!.Id }, result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(InventoryPermissions.Transfer)]
    [HttpPost("traslados/{id:int}/anular")]
    [AuditEntity("TrasladoInventario", "ANULAR")]
    [ProducesResponseType(typeof(MovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovementResponse>> AnnulTransfer(
        int id,
        [FromBody] AnnulTransferRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<MovementResponse> result =
            await _transfers.AnnulAsync(id, request, this.GetAuthenticatedUserId(), CanViewCosts, cancellationToken);

        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}
