using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NovaBooks.Application.Common;
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

    [HasPermission("Usuarios.Ver")]
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<UserResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<UserResponse>>> GetPaged(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
    {
        PagedResponse<UserResponse> response =
            await _userService.GetPagedAsync(
                search,
                page,
                pageSize,
                cancellationToken);

        return Ok(response);
    }

    [HasPermission("Usuarios.Ver")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
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
            return NotFound(new
            {
                message =
                    "El usuario solicitado no existe."
            });
        }

        return Ok(user);
    }

    [HasPermission("Usuarios.Crear")]
    [HttpPost]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        UserOperationResult<UserResponse> result =
            await _userService.CreateAsync(
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

    [HasPermission("Usuarios.Modificar")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        UserOperationResult<UserResponse> result =
            await _userService.UpdateAsync(
                id,
                request,
                cancellationToken);

        return FromResult(result);
    }

    [HasPermission("Usuarios.Desactivar")]
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> ChangeStatus(
        int id,
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(
                out int authenticatedUserId))
        {
            return Unauthorized(new
            {
                message =
                    "No fue posible identificar al usuario autenticado."
            });
        }

        UserOperationResult<UserResponse> result =
            await _userService.ChangeStatusAsync(
                id,
                request.IsActive,
                authenticatedUserId,
                cancellationToken);

        return FromResult(result);
    }

    [HasPermission("Usuarios.AsignarRoles")]
    [HttpPut("{id:int}/roles")]
    [ProducesResponseType(
        typeof(UserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> AssignRoles(
        int id,
        [FromBody] AssignUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        UserOperationResult<UserResponse> result =
            await _userService.AssignRolesAsync(
                id,
                request,
                cancellationToken);

        return FromResult(result);
    }

    [HasPermission("Usuarios.RestablecerPassword")]
    [HttpPut("{id:int}/password")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        int id,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        UserOperationResult<bool> result =
            await _userService.ResetPasswordAsync(
                id,
                request,
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

    private ActionResult<UserResponse> FromResult(
        UserOperationResult<UserResponse> result)
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

    private bool TryGetAuthenticatedUserId(
        out int userId)
    {
        string? value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return int.TryParse(value, out userId);
    }
}