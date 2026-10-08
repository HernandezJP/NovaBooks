using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaBooks.Application.Authentication;
using NovaBooks.Infrastructure.Security.Permissions;

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
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        LoginResult result =
            await _authenticationService.LoginAsync(
                request,
                cancellationToken);

        return result.Status switch
        {
            LoginStatus.Succeeded => Ok(result.Response),

            LoginStatus.LockedOut => LoginProblem(
                "account_locked",
                BuildLockoutMessage(result.LockoutEndUtc)),

            LoginStatus.Disabled => LoginProblem(
                "account_disabled",
                "La cuenta se encuentra desactivada. " +
                "Comuníquese con un administrador."),

            _ => LoginProblem(
                "invalid_credentials",
                "El usuario o la contraseña son incorrectos.")
        };
    }

    private ObjectResult LoginProblem(
        string code,
        string detail)
    {
        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "No fue posible iniciar sesión.",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };

        problem.Extensions["code"] = code;

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status401Unauthorized,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static string BuildLockoutMessage(
        DateTimeOffset? lockoutEndUtc)
    {
        double remaining = lockoutEndUtc.HasValue
            ? (lockoutEndUtc.Value - DateTimeOffset.UtcNow).TotalMinutes
            : 1;

        int minutes = Math.Max(1, (int)Math.Ceiling(remaining));

        string unit = minutes == 1 ? "minuto" : "minutos";

        return "La cuenta está bloqueada temporalmente. " +
               $"Intente nuevamente en {minutes} {unit}.";
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
            .FindAll(CustomClaimTypes.Permission)
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