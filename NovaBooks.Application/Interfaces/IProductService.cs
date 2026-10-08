using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Products;

namespace NovaBooks.Application.Interfaces;

/// <summary>
/// includeCosts indica si el usuario puede ver el costo de referencia
/// (Catalogo.AdministrarPrecios).
/// </summary>
public interface IProductService
{
    Task<PagedResponse<ProductListItemResponse>> GetPagedAsync(
        ProductQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<ProductCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<ProductResponse>> CreateAsync(
        SaveProductRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ProductResponse>> UpdateAsync(
        int id,
        SaveProductRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<OperationResult<ProductResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        bool includeCosts,
        CancellationToken cancellationToken = default);
}
