using NovaBooks.Application.DTOs.Permissions;

namespace NovaBooks.Application.Interfaces;

public interface IPermissionService
{
    IReadOnlyCollection<PermissionResponse> GetAll();

    IReadOnlyCollection<PermissionGroupResponse> GetGrouped();
}