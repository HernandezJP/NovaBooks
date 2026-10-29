using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;

namespace TikalLibros.Application.Interfaces;

public interface IReceptionService
{
    Task<PagedResponse<ReceptionListItemResponse>> GetPagedAsync(
        ReceptionQuery query,
        CancellationToken cancellationToken = default);

    Task<ReceptionResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ReceptionCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra lo recibido de una orden emitida y, en la misma transacción,
    /// crea los lotes, suma las existencias, genera la entrada de inventario y
    /// actualiza el estado de la orden.
    /// </summary>
    Task<OperationResult<ReceptionResponse>> RegisterAsync(
        RegisterReceptionRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Crea la orden emitida y la recibe completa en un paso.</summary>
    Task<OperationResult<ReceptionResponse>> RegisterDirectPurchaseAsync(
        DirectPurchaseRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revierte la entrada si los lotes no han tenido salidas.
    /// </summary>
    Task<OperationResult<ReceptionResponse>> AnnulAsync(
        int id,
        AnnulReceptionRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
