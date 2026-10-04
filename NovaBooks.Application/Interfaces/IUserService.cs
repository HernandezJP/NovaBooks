using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Users;

namespace NovaBooks.Application.Interfaces;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<UserResponse>> AssignRolesAsync(
        int id,
        AssignUserRolesRequest request,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult<bool>> ResetPasswordAsync(
        int id,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default);
}