using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;

namespace TikalLibros.Application.Interfaces;

public interface IPurchaseOrderService
{
    Task<PagedResponse<PurchaseOrderListItemResponse>> GetPagedAsync(
        PurchaseOrderQuery query,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Libros activos para agregar a una orden (máximo 20).</summary>
    Task<IReadOnlyCollection<PurchaseBookOptionResponse>> SearchBooksAsync(
        string? search,
        CancellationToken cancellationToken = default);

    /// <summary>Crea la orden en estado borrador.</summary>
    Task<OperationResult<PurchaseOrderResponse>> CreateAsync(
        SavePurchaseOrderRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Solo se modifican órdenes en borrador.</summary>
    Task<OperationResult<PurchaseOrderResponse>> UpdateAsync(
        int id,
        SavePurchaseOrderRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Borrador → emitida; desde ahí puede recibirse.</summary>
    Task<OperationResult<PurchaseOrderResponse>> IssueAsync(
        int id,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Solo órdenes sin unidades recibidas.</summary>
    Task<OperationResult<PurchaseOrderResponse>> CancelAsync(
        int id,
        CancelPurchaseOrderRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
