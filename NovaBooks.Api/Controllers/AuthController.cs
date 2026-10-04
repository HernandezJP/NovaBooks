using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaBooks.Application.Authentication;

namespace NovaBooks.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(LoginResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        LoginResponse? response =
            await _authenticationService.LoginAsync(
                request,
                cancellationToken);

        if (response is null)
        {
            return Unauthorized(new
            {
                message = "El usuario o la contraseña son incorrectos."
            });
        }

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(AuthenticatedUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthenticatedUserResponse> Me()
    {
        string id =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        string userName =
            User.FindFirstValue(ClaimTypes.Name)
            ?? string.Empty;

        string email =
            User.FindFirstValue(ClaimTypes.Email)
            ?? string.Empty;

        string[] roles = User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role)
            .ToArray();

        string[] permissions = User
            .FindAll("permission")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray();

        return Ok(new AuthenticatedUserResponse
        {
            Id = id,
            UserName = userName,
            Email = email,
            Roles = roles,
            Permissions = permissions
        });
    }
}