using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;

namespace TikalLibros.Application.Interfaces;

/// <summary>
/// Traslados de existencias entre almacenes. Cada traslado es un movimiento
/// de inventario TRANSFERENCIA que se procesa al registrarse.
/// </summary>
public interface ITransferService
{
    Task<PagedResponse<TransferListItemResponse>> GetPagedAsync(
        TransferQuery query,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MovementResponse>> CreateAsync(
        SaveTransferRequest request,
        int authenticatedUserId,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<OperationResult<MovementResponse>> AnnulAsync(
        int id,
        AnnulTransferRequest request,
        int authenticatedUserId,
        bool includeCosts,
        CancellationToken cancellationToken = default);
}
