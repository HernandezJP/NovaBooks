using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Roles;

namespace NovaBooks.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<RoleResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<RoleOperationResult<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<RoleOperationResult<RoleResponse>> UpdateAsync(
        int id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<RoleOperationResult<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<RoleOperationResult<RoleResponse>> AssignPermissionsAsync(
        int id,
        AssignRolePermissionsRequest request,
        CancellationToken cancellationToken = default);
}