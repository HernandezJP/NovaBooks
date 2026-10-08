using Microsoft.AspNetCore.Mvc;
using NovaBooks.Api.Common;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

[ApiController]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(
        IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HasPermission(RolePermissions.View)]
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<RoleResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyCollection<RoleResponse>>> GetAll(
            CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetAllAsync(
            cancellationToken));
    }

    [HasPermission(RolePermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        RoleResponse? role =
            await _roleService.GetByIdAsync(
                id,
                cancellationToken);

        if (role is null)
        {
            return this.NotFoundProblem(
                "El rol solicitado no existe.");
        }

        return Ok(role);
    }

    [HasPermission(RolePermissions.View)]
    [HttpGet("{id:int}/usuarios")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<RoleUserResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<IReadOnlyCollection<RoleUserResponse>>> GetUsers(
            int id,
            CancellationToken cancellationToken)
    {
        OperationResult<IReadOnlyCollection<RoleUserResponse>> result =
            await _roleService.GetUsersAsync(
                id,
                cancellationToken);

        return result.Succeeded
            ? Ok(result.Data)
            : this.OperationProblem(result);
    }

    [HasPermission(RolePermissions.Create)]
    [HttpPost]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<RoleResponse> result =
            await _roleService.CreateAsync(
                request,
                cancellationToken);

        if (!result.Succeeded)
        {
            return this.OperationProblem(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Data!.Id
            },
            result.Data);
    }

    [HasPermission(RolePermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>> Update(
        int id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(
            await _roleService.UpdateAsync(
                id,
                request,
                cancellationToken));
    }

    /// <summary>
    /// Desactivación lógica del rol (ROL_Activo = false).
    /// </summary>
    [HasPermission(RolePermissions.Delete)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        OperationResult<RoleResponse> result =
            await _roleService.ChangeStatusAsync(
                id,
                isActive: false,
                cancellationToken);

        return result.Succeeded
            ? NoContent()
            : this.OperationProblem(result);
    }

    /// <summary>
    /// Reactivar requiere Roles.Modificar; desactivar, Roles.Eliminar.
    /// </summary>
    [HasAnyPermission(
        RolePermissions.Update,
        RolePermissions.Delete)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeRoleStatusRequest request,
        CancellationToken cancellationToken)
    {
        string required = request.IsActive
            ? RolePermissions.Update
            : RolePermissions.Delete;

        if (!this.HasPermission(required))
        {
            return this.ForbiddenProblem(
                $"Se requiere el permiso {required}.");
        }

        return FromResult(
            await _roleService.ChangeStatusAsync(
                id,
                request.IsActive,
                cancellationToken));
    }

    [HasPermission(RolePermissions.AssignPermissions)]
    [HttpPut("{id:int}/permisos")]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>>
        AssignPermissions(
            int id,
            [FromBody] AssignRolePermissionsRequest request,
            CancellationToken cancellationToken)
    {
        return FromResult(
            await _roleService.AssignPermissionsAsync(
                id,
                request,
                cancellationToken));
    }

    private ActionResult<RoleResponse> FromResult(
        OperationResult<RoleResponse> result)
    {
        return result.Succeeded
            ? Ok(result.Data)
            : this.OperationProblem(result);
    }
}
