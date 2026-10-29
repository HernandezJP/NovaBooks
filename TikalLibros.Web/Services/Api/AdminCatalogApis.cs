using TikalLibros.Application.DTOs.Categories;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.DTOs.PriceLists;

namespace TikalLibros.Web.Services.Api;

/// <summary>Catálogos base que se listan completos (sin paginación).</summary>
public abstract class AdminCatalogApi<TResponse, TSave>(ApiClient api, string path)
    where TSave : class
{
    protected ApiClient Api { get; } = api;

    public Task<ApiResult<List<TResponse>>> GetAllAsync(
        string? search, StatusFilter status, CancellationToken cancellationToken = default) =>
        Api.GetAsync<List<TResponse>>(
            QueryString.Build(path,
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status))),
            cancellationToken);

    public Task<ApiResult<TResponse>> CreateAsync(TSave request) =>
        Api.PostAsync<TResponse>(path, request);

    public Task<ApiResult<TResponse>> UpdateAsync(int id, TSave request) =>
        Api.PutAsync<TResponse>($"{path}/{id}", request);

    public Task<ApiResult<TResponse>> ChangeStatusAsync(int id, bool isActive) =>
        Api.PatchAsync<TResponse>($"{path}/{id}/estado", new ChangeActiveStatusRequest { IsActive = isActive });
}

public sealed class CategoriesApi(ApiClient api)
    : AdminCatalogApi<CategoryResponse, SaveCategoryRequest>(api, "api/categorias");

public sealed class PriceListsApi(ApiClient api)
    : AdminCatalogApi<PriceListResponse, SavePriceListRequest>(api, "api/listas-precio");

public sealed class BranchesApi(ApiClient api)
    : AdminCatalogApi<BranchResponse, SaveBranchRequest>(api, "api/sucursales")
{
    public Task<ApiResult<OrganizationCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        Api.GetAsync<OrganizationCatalogsResponse>("api/sucursales/catalogos", cancellationToken);
}

public sealed class WarehousesApi(ApiClient api)
    : AdminCatalogApi<WarehouseResponse, SaveWarehouseRequest>(api, "api/almacenes");

public sealed class CashRegistersApi(ApiClient api)
    : AdminCatalogApi<CashRegisterAdminResponse, SaveCashRegisterRequest>(api, "api/cajas");
