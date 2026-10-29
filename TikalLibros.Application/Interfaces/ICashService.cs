using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;

namespace TikalLibros.Application.Interfaces;

/// <summary>Qué puede hacer el usuario en caja además de lo que pide cada endpoint.</summary>
public sealed record CashPermissionSet(bool CanRegisterIncome, bool CanRegisterExpense, bool CanSupervise);

public interface ICashService
{
    Task<CashCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResponse<CashSessionListItemResponse>> GetSessionsAsync(
        CashSessionQuery query,
        CancellationToken cancellationToken = default);

    Task<CashSessionResponse?> GetSessionAsync(
        int id,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>La apertura abierta del empleado del usuario, si tiene una.</summary>
    Task<CashSessionResponse?> GetCurrentSessionAsync(
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CashSessionResponse>> OpenAsync(
        OpenCashSessionRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CashSessionResponse>> RegisterMovementAsync(
        int sessionId,
        CashMovementRequest request,
        int authenticatedUserId,
        CashPermissionSet permissions,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CashSessionResponse>> AnnulMovementAsync(
        int movementId,
        AnnulCashMovementRequest request,
        int authenticatedUserId,
        CashPermissionSet permissions,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CashSessionResponse>> CloseAsync(
        int sessionId,
        CloseCashSessionRequest request,
        int authenticatedUserId,
        CashPermissionSet permissions,
        CancellationToken cancellationToken = default);
}
