using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Sales;

namespace TikalLibros.Application.Interfaces;

/// <summary>Devoluciones de clientes (notas de crédito) sobre ventas.</summary>
public interface IReturnService
{
    Task<ReturnCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default);

    Task<PagedResponse<ReturnListItemResponse>> GetPagedAsync(
        ReturnQuery query,
        CancellationToken cancellationToken = default);

    Task<ReturnResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<ReturnResponse>> CreateAsync(
        SaveReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ReturnResponse>> AnnulAsync(
        int id,
        AnnulReturnRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
