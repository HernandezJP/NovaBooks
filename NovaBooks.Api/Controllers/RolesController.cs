using Microsoft.AspNetCore.Mvc;
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

    [HasPermission("Roles.Ver")]
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<RoleResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyCollection<RoleResponse>>> GetAll(
            CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RoleResponse> roles =
            await _roleService.GetAllAsync(
                cancellationToken);

        return Ok(roles);
    }

    [HasPermission("Roles.Ver")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
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
            return NotFound(new
            {
                message =
                    "El rol solicitado no existe."
            });
        }

        return Ok(role);
    }

    [HasPermission("Roles.Crear")]
    [HttpPost]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoleResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        RoleOperationResult<RoleResponse> result =
            await _roleService.CreateAsync(
                request,
                cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Data!.Id
            },
            result.Data);
    }

    [HasPermission("Roles.Modificar")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>> Update(
        int id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        RoleOperationResult<RoleResponse> result =
            await _roleService.UpdateAsync(
                id,
                request,
                cancellationToken);

        return FromResult(result);
    }

    [HasPermission("Roles.Eliminar")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        RoleOperationResult<bool> result =
            await _roleService.DeleteAsync(
                id,
                cancellationToken);

        if (result.NotFound)
        {
            return NotFound(new
            {
                errors = result.Errors
            });
        }

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors
            });
        }

        return NoContent();
    }

    [HasPermission("Roles.AsignarPermisos")]
    [HttpPut("{id:int}/permisos")]
    [ProducesResponseType(
        typeof(RoleResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>>
        AssignPermissions(
            int id,
            [FromBody] AssignRolePermissionsRequest request,
            CancellationToken cancellationToken)
    {
        RoleOperationResult<RoleResponse> result =
            await _roleService.AssignPermissionsAsync(
                id,
                request,
                cancellationToken);

        return FromResult(result);
    }

    private ActionResult<RoleResponse> FromResult(
        RoleOperationResult<RoleResponse> result)
    {
        if (result.NotFound)
        {
            return NotFound(new
            {
                errors = result.Errors
            });
        }

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors
            });
        }

        return Ok(result.Data);
    }
}