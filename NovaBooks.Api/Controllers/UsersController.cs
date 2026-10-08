using Microsoft.AspNetCore.Mvc;
using NovaBooks.Api.Common;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.DTOs.Users;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService)
    {
        _userService = userService;
    }

    [HasPermission(UserPermissions.View)]
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<UserResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<UserResponse>>> GetPaged(
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
    {
        PagedResponse<UserResponse> response =
            await _userService.GetPagedAsync(
                search,
                isActive,
                page,
                pageSize,
                cancellationToken);

        return Ok(response);
    }

    [HasPermission(UserPermissions.View)]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        UserResponse? user =
            await _userService.GetByIdAsync(
                id,
                cancellationToken);

        if (user is null)
        {
            return this.NotFoundProblem(
                "El usuario solicitado no existe.");
        }

        return Ok(user);
    }

    [HasAnyPermission(
        UserPermissions.View,
        UserPermissions.Create,
        UserPermissions.AssignRoles)]
    [HttpGet("catalogos/roles")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<RoleOptionResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyCollection<RoleOptionResponse>>>
        GetAssignableRoles(
            CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetAssignableRolesAsync(
            cancellationToken));
    }

    [HasPermission(UserPermissions.Create)]
    [HttpPost]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Roles.Any(role => !string.IsNullOrWhiteSpace(role)) &&
            !this.HasPermission(UserPermissions.AssignRoles))
        {
            return this.ForbiddenProblem(
                "Se requiere el permiso Usuarios.AsignarRoles " +
                "para crear usuarios con roles.");
        }

        OperationResult<UserResponse> result =
            await _userService.CreateAsync(
                request,
                this.GetAuthenticatedUserId(),
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

    [HasPermission(UserPermissions.Update)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(UserResponse),
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
    public async Task<ActionResult<UserResponse>> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(
            await _userService.UpdateAsync(
                id,
                request,
                this.GetAuthenticatedUserId(),
                cancellationToken));
    }

    [HasPermission(UserPermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(
            await _userService.ChangeStatusAsync(
                id,
                request.IsActive,
                this.GetAuthenticatedUserId(),
                cancellationToken));
    }

    [HasPermission(UserPermissions.AssignRoles)]
    [HttpPut("{id:int}/roles")]
    [ProducesResponseType(
        typeof(UserResponse),
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
    public async Task<ActionResult<UserResponse>> AssignRoles(
        int id,
        [FromBody] AssignUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        return FromResult(
            await _userService.AssignRolesAsync(
                id,
                request,
                this.GetAuthenticatedUserId(),
                cancellationToken));
    }

    [HasPermission(UserPermissions.ResetPassword)]
    [HttpPut("{id:int}/password")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        int id,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        OperationResult<bool> result =
            await _userService.ResetPasswordAsync(
                id,
                request,
                this.GetAuthenticatedUserId(),
                cancellationToken);

        return result.Succeeded
            ? NoContent()
            : this.OperationProblem(result);
    }

    private ActionResult<UserResponse> FromResult(
        OperationResult<UserResponse> result)
    {
        return result.Succeeded
            ? Ok(result.Data)
            : this.OperationProblem(result);
    }
}
