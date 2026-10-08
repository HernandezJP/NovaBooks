using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.DTOs.Users;

namespace NovaBooks.Application.Interfaces;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoleOptionResponse>> GetAssignableRolesAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<UserResponse>> UpdateAsync(
        int id,
        UpdateUserRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<UserResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<UserResponse>> AssignRolesAsync(
        int id,
        AssignUserRolesRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ResetPasswordAsync(
        int id,
        ResetUserPasswordRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
