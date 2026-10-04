using Microsoft.AspNetCore.Mvc;
using NovaBooks.Application.DTOs.Permissions;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

[ApiController]
[Route("api/permisos")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(
        IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HasPermission("Roles.Ver")]
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<PermissionResponse>),
        StatusCodes.Status200OK)]
    public ActionResult<
        IReadOnlyCollection<PermissionResponse>> GetAll()
    {
        return Ok(_permissionService.GetAll());
    }

    [HasPermission("Roles.Ver")]
    [HttpGet("agrupados")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<PermissionGroupResponse>),
        StatusCodes.Status200OK)]
    public ActionResult<
        IReadOnlyCollection<PermissionGroupResponse>> GetGrouped()
    {
        return Ok(_permissionService.GetGrouped());
    }
}