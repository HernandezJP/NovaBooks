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

    Task<OperationResult<IReadOnlyCollection<RoleUserResponse>>>
        GetUsersAsync(
            int id,
            CancellationToken cancellationToken = default);

    Task<OperationResult<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<RoleResponse>> UpdateAsync(
        int id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<RoleResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<OperationResult<RoleResponse>> AssignPermissionsAsync(
        int id,
        AssignRolePermissionsRequest request,
        CancellationToken cancellationToken = default);
}
