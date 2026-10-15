using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Suppliers;

namespace TikalLibros.Application.Interfaces;

public interface ISupplierService
{
    Task<PagedResponse<SupplierListItemResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        string? personType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SupplierResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<SupplierCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<SupplierResponse>> CreateAsync(
        SaveSupplierRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<SupplierResponse>> UpdateAsync(
        int id,
        SaveSupplierRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Desactivación lógica; no se permite con órdenes de compra abiertas.
    /// </summary>
    Task<OperationResult<SupplierResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
