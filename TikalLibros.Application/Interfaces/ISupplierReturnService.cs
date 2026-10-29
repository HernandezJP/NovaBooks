using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;

namespace TikalLibros.Application.Interfaces;

/// <summary>
/// Devoluciones de mercadería al proveedor sobre lo recibido en una recepción.
/// Cada devolución es un movimiento de inventario DEVOLUCION_PROVEEDOR.
/// </summary>
public interface ISupplierReturnService
{
    Task<PagedResponse<SupplierReturnListItemResponse>> GetPagedAsync(
        SupplierReturnQuery query,
        CancellationToken cancellationToken = default);

    Task<SupplierReturnResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ReturnableReceptionResponse?> GetReturnableAsync(int receptionId, CancellationToken cancellationToken = default);

    Task<OperationResult<SupplierReturnResponse>> CreateAsync(
        SaveSupplierReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<SupplierReturnResponse>> AnnulAsync(
        int id,
        AnnulSupplierReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
