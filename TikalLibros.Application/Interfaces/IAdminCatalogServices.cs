using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Categories;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.DTOs.PriceLists;

namespace TikalLibros.Application.Interfaces;

/// <summary>
/// Catálogos base de pocos registros (categorías, listas de precio,
/// sucursales, almacenes y cajas): se listan completos, sin paginación, y se
/// desactivan en lugar de eliminarse.
/// </summary>
public interface IAdminCatalogService<TResponse, in TSave>
{
    Task<IReadOnlyList<TResponse>> GetAllAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<TResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult<TResponse>> CreateAsync(
        TSave request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<TResponse>> UpdateAsync(
        int id,
        TSave request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<TResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}

public interface ICategoryService : IAdminCatalogService<CategoryResponse, SaveCategoryRequest>;

public interface IPriceListService : IAdminCatalogService<PriceListResponse, SavePriceListRequest>;

public interface IBranchService : IAdminCatalogService<BranchResponse, SaveBranchRequest>
{
    /// <summary>Departamentos con sus municipios y sucursales activas.</summary>
    Task<OrganizationCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default);
}

public interface IWarehouseService : IAdminCatalogService<WarehouseResponse, SaveWarehouseRequest>;

public interface ICashRegisterService : IAdminCatalogService<CashRegisterAdminResponse, SaveCashRegisterRequest>;
