using Microsoft.AspNetCore.Mvc;
using TikalLibros.Application.DTOs.Permissions;
using TikalLibros.Application.DTOs.Roles;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

[ApiController]
[Route("api/permisos")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    private readonly IRoleService _roleService;

    public PermissionsController(
        IPermissionService permissionService,
        IRoleService roleService)
    {
        _permissionService = permissionService;
        _roleService = roleService;
    }

    [HasAnyPermission(
        RolePermissions.View,
        RolePermissions.AssignPermissions)]
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<PermissionResponse>),
        StatusCodes.Status200OK)]
    public ActionResult<
        IReadOnlyCollection<PermissionResponse>> GetAll()
    {
        return Ok(_permissionService.GetAll());
    }

    [HasAnyPermission(
        RolePermissions.View,
        RolePermissions.AssignPermissions)]
    [HttpGet("agrupados")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<PermissionGroupResponse>),
        StatusCodes.Status200OK)]
    public ActionResult<
        IReadOnlyCollection<PermissionGroupResponse>> GetGrouped()
    {
        return Ok(_permissionService.GetGrouped());
    }

    /// <summary>
    /// Roles con sus permisos, para la matriz de asignación.
    /// </summary>
    [HasPermission(RolePermissions.AssignPermissions)]
    [HttpGet("roles")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<RoleResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyCollection<RoleResponse>>> GetRoles(
            CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetAllAsync(
            cancellationToken));
    }
}
