using NovaBooks.Application.DTOs.Permissions;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    public IReadOnlyCollection<PermissionResponse> GetAll()
    {
        return SystemPermissions
            .GetAll()
            .OrderBy(permission => permission)
            .Select(MapPermission)
            .ToArray();
    }

    public IReadOnlyCollection<PermissionGroupResponse> GetGrouped()
    {
        return SystemPermissions
            .GetAll()
            .Select(MapPermission)
            .GroupBy(permission => permission.Module)
            .OrderBy(group => group.Key)
            .Select(group => new PermissionGroupResponse
            {
                Module = group.Key,

                Permissions = group
                    .OrderBy(permission => permission.Action)
                    .ToArray()
            })
            .ToArray();
    }

    private static PermissionResponse MapPermission(
        string permissionCode)
    {
        string[] parts =
            permissionCode.Split(
                '.',
                2,
                StringSplitOptions.TrimEntries);

        return new PermissionResponse
        {
            Code = permissionCode,

            Module = parts.Length > 0
                ? parts[0]
                : permissionCode,

            Action = parts.Length > 1
                ? parts[1]
                : permissionCode
        };
    }
}