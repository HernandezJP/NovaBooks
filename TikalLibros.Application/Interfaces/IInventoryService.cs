using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;

namespace TikalLibros.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResponse<StockItemResponse>> GetStockAsync(
        StockQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    /// <summary>Configura mínimo, máximo y ubicación (crea la existencia si no hay).</summary>
    Task<OperationResult<StockItemResponse>> SaveLevelsAsync(
        SaveStockLevelsRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockAlertResponse>> GetAlertsAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default);

    Task<StockAlertSummaryResponse> GetAlertSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResponse<MovementListItemResponse>> GetMovementsAsync(
        MovementQuery query,
        CancellationToken cancellationToken = default);

    Task<MovementResponse?> GetMovementAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<OperationResult<KardexResponse>> GetKardexAsync(
        KardexQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<AdjustmentListItemResponse>> GetAdjustmentsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdjustmentResponse?> GetAdjustmentAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra el ajuste, su movimiento y los cambios en lotes y existencias
    /// en una transacción. Las salidas consumen los lotes más antiguos.
    /// </summary>
    Task<OperationResult<AdjustmentResponse>> CreateAdjustmentAsync(
        SaveAdjustmentRequest request,
        int authenticatedUserId,
        bool includeCosts,
        CancellationToken cancellationToken = default);
}
