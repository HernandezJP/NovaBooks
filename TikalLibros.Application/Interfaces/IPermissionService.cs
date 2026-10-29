using TikalLibros.Application.DTOs.Permissions;

namespace TikalLibros.Application.Interfaces;

public interface IPermissionService
{
    IReadOnlyCollection<PermissionResponse> GetAll();

    IReadOnlyCollection<PermissionGroupResponse> GetGrouped();
}