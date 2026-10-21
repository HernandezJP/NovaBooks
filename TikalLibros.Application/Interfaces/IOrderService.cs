using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Sales;

namespace TikalLibros.Application.Interfaces;

/// <summary>Permisos del usuario que cambian lo que puede hacer en un pedido.</summary>
public sealed record SalesPermissionSet(bool CanApplyDiscount, bool CanSellOnCredit, bool CanApprove);

public interface IOrderService
{
    Task<OrderCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SalesBookOptionResponse>> SearchBooksAsync(
        string? search,
        int? priceListId,
        int? warehouseId,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<OrderListItemResponse>> GetPagedAsync(
        OrderQuery query,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> GetByIdAsync(
        int id,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderResponse>> CreateAsync(
        SaveOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderResponse>> UpdateAsync(
        int id,
        SaveOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    /// <summary>Valida la existencia y reserva, o deja el pedido por aprobar.</summary>
    Task<OperationResult<OrderResponse>> ConfirmAsync(
        int id,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderResponse>> ApproveAsync(
        int id,
        ApproveOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderResponse>> RejectAsync(
        int id,
        OrderReasonRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    Task<OperationResult<OrderResponse>> CancelAsync(
        int id,
        OrderReasonRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    /// <summary>Pasa a vencidos los pedidos cuya reserva expiró y libera la mercadería.</summary>
    Task<int> ExpireReservationsAsync(
        CancellationToken cancellationToken = default);
}
