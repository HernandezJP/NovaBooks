using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Authors;
using NovaBooks.Application.DTOs.Customers;
using NovaBooks.Application.DTOs.Editorials;
using NovaBooks.Application.DTOs.Menu;
using NovaBooks.Application.DTOs.Permissions;
using NovaBooks.Application.DTOs.Products;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.DTOs.Users;

namespace NovaBooks.Web.Services.Api;

/// <summary>
/// Filtro de estado común en los listados.
/// </summary>
public enum StatusFilter
{
    Active,
    Inactive,
    All
}

internal static class QueryString
{
    public static string Build(string path, params (string Name, object? Value)[] parameters)
    {
        string query = string.Join("&", parameters
            .Where(parameter => parameter.Value is not null &&
                                parameter.Value is not string { Length: 0 })
            .Select(parameter =>
                $"{parameter.Name}={Uri.EscapeDataString(Format(parameter.Value!))}"));

        return query.Length == 0 ? path : $"{path}?{query}";
    }

    public static bool? ToIsActive(StatusFilter filter)
    {
        return filter switch
        {
            StatusFilter.Active => true,
            StatusFilter.Inactive => false,
            _ => null
        };
    }

    private static string Format(object value)
    {
        return value switch
        {
            bool flag => flag ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}

public sealed class MenuApi(ApiClient api)
{
    public Task<ApiResult<List<MenuGroupResponse>>> GetAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<List<MenuGroupResponse>>("api/menu", cancellationToken);
}

public sealed class UsersApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<UserResponse>>> GetPagedAsync(
        string? search, StatusFilter status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<UserResponse>>(
            QueryString.Build("api/usuarios",
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status)),
                ("page", page),
                ("pageSize", pageSize)),
            cancellationToken);

    public Task<ApiResult<List<RoleOptionResponse>>> GetAssignableRolesAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<List<RoleOptionResponse>>("api/usuarios/catalogos/roles", cancellationToken);

    public Task<ApiResult<UserResponse>> CreateAsync(CreateUserRequest request) =>
        api.PostAsync<UserResponse>("api/usuarios", request);

    public Task<ApiResult<UserResponse>> UpdateAsync(int id, UpdateUserRequest request) =>
        api.PutAsync<UserResponse>($"api/usuarios/{id}", request);

    public Task<ApiResult<UserResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<UserResponse>($"api/usuarios/{id}/estado", new ChangeUserStatusRequest { IsActive = isActive });

    public Task<ApiResult<UserResponse>> AssignRolesAsync(int id, IEnumerable<string> roles) =>
        api.PutAsync<UserResponse>($"api/usuarios/{id}/roles", new AssignUserRolesRequest { Roles = roles.ToList() });

    public Task<ApiResult<bool>> ResetPasswordAsync(int id, string newPassword) =>
        api.PutAsync<bool>($"api/usuarios/{id}/password", new ResetUserPasswordRequest { NewPassword = newPassword });
}

public sealed class RolesApi(ApiClient api)
{
    public Task<ApiResult<List<RoleResponse>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<List<RoleResponse>>("api/roles", cancellationToken);

    public Task<ApiResult<List<RoleUserResponse>>> GetUsersAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<List<RoleUserResponse>>($"api/roles/{id}/usuarios", cancellationToken);

    public Task<ApiResult<RoleResponse>> CreateAsync(CreateRoleRequest request) =>
        api.PostAsync<RoleResponse>("api/roles", request);

    public Task<ApiResult<RoleResponse>> UpdateAsync(int id, UpdateRoleRequest request) =>
        api.PutAsync<RoleResponse>($"api/roles/{id}", request);

    public Task<ApiResult<RoleResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<RoleResponse>($"api/roles/{id}/estado", new ChangeRoleStatusRequest { IsActive = isActive });
}

public sealed class PermissionsApi(ApiClient api)
{
    public Task<ApiResult<List<PermissionGroupResponse>>> GetGroupedAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<List<PermissionGroupResponse>>("api/permisos/agrupados", cancellationToken);

    public Task<ApiResult<List<RoleResponse>>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<List<RoleResponse>>("api/permisos/roles", cancellationToken);

    public Task<ApiResult<RoleResponse>> AssignAsync(int roleId, IEnumerable<string> permissions) =>
        api.PutAsync<RoleResponse>($"api/roles/{roleId}/permisos",
            new AssignRolePermissionsRequest { Permissions = permissions.ToList() });
}

public sealed class CustomersApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<CustomerListItemResponse>>> GetPagedAsync(
        string? search, StatusFilter status, string? personType, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<CustomerListItemResponse>>(
            QueryString.Build("api/clientes",
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status)),
                ("personType", personType),
                ("page", page),
                ("pageSize", pageSize)),
            cancellationToken);

    public Task<ApiResult<CustomerResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<CustomerResponse>($"api/clientes/{id}", cancellationToken);

    public Task<ApiResult<CustomerCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<CustomerCatalogsResponse>("api/clientes/catalogos", cancellationToken);

    public Task<ApiResult<CustomerResponse>> CreateAsync(SaveCustomerRequest request) =>
        api.PostAsync<CustomerResponse>("api/clientes", request);

    public Task<ApiResult<CustomerResponse>> UpdateAsync(int id, SaveCustomerRequest request) =>
        api.PutAsync<CustomerResponse>($"api/clientes/{id}", request);

    public Task<ApiResult<CustomerResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<CustomerResponse>($"api/clientes/{id}/estado", new ChangeCustomerStatusRequest { IsActive = isActive });
}

public sealed class ProductsApi(ApiClient api, Microsoft.Extensions.Options.IOptions<ApiOptions> options)
{
    /// <summary>
    /// URL pública de la portada; la versión evita mostrar una imagen en caché.
    /// </summary>
    public string? ImageUrl(int productId, string? version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return null;
        }

        string baseUrl = options.Value.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/api/productos/{productId}/imagen?v={Uri.EscapeDataString(version)}";
    }

    public Task<ApiResult<ProductResponse>> UploadImageAsync(int id, Stream content, string fileName, string contentType) =>
        api.PostFileAsync<ProductResponse>($"api/productos/{id}/imagen", content, fileName, contentType);

    public Task<ApiResult<bool>> DeleteImageAsync(int id) =>
        api.DeleteAsync($"api/productos/{id}/imagen");

    public Task<ApiResult<PagedResponse<ProductListItemResponse>>> GetPagedAsync(
        string? search, StatusFilter status, int? categoryId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<ProductListItemResponse>>(
            QueryString.Build("api/productos",
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status)),
                ("categoryId", categoryId),
                ("page", page),
                ("pageSize", pageSize)),
            cancellationToken);

    public Task<ApiResult<ProductResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<ProductResponse>($"api/productos/{id}", cancellationToken);

    public Task<ApiResult<ProductCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<ProductCatalogsResponse>("api/productos/catalogos", cancellationToken);

    public Task<ApiResult<ProductResponse>> CreateAsync(SaveProductRequest request) =>
        api.PostAsync<ProductResponse>("api/productos", request);

    public Task<ApiResult<ProductResponse>> UpdateAsync(int id, SaveProductRequest request) =>
        api.PutAsync<ProductResponse>($"api/productos/{id}", request);

    public Task<ApiResult<ProductResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<ProductResponse>($"api/productos/{id}/estado", new ChangeProductStatusRequest { IsActive = isActive });
}

public sealed class AuthorsApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<AuthorResponse>>> GetPagedAsync(
        string? search, StatusFilter status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<AuthorResponse>>(
            QueryString.Build("api/autores",
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status)),
                ("page", page),
                ("pageSize", pageSize)),
            cancellationToken);

    public Task<ApiResult<AuthorResponse>> CreateAsync(SaveAuthorRequest request) =>
        api.PostAsync<AuthorResponse>("api/autores", request);

    public Task<ApiResult<AuthorResponse>> UpdateAsync(int id, SaveAuthorRequest request) =>
        api.PutAsync<AuthorResponse>($"api/autores/{id}", request);

    public Task<ApiResult<AuthorResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<AuthorResponse>($"api/autores/{id}/estado", new ChangeAuthorStatusRequest { IsActive = isActive });
}

public sealed class EditorialsApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<EditorialResponse>>> GetPagedAsync(
        string? search, StatusFilter status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<EditorialResponse>>(
            QueryString.Build("api/editoriales",
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status)),
                ("page", page),
                ("pageSize", pageSize)),
            cancellationToken);

    public Task<ApiResult<EditorialResponse>> CreateAsync(SaveEditorialRequest request) =>
        api.PostAsync<EditorialResponse>("api/editoriales", request);

    public Task<ApiResult<EditorialResponse>> UpdateAsync(int id, SaveEditorialRequest request) =>
        api.PutAsync<EditorialResponse>($"api/editoriales/{id}", request);

    public Task<ApiResult<EditorialResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<EditorialResponse>($"api/editoriales/{id}/estado", new ChangeEditorialStatusRequest { IsActive = isActive });
}
