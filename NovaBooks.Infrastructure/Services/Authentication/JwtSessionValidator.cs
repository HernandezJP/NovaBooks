using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Infrastructure.Services.Authentication;

/// <summary>
/// Valida en cada solicitud que la sesión del JWT siga vigente y
/// reemplaza roles y permisos del token por los actuales.
/// </summary>
public static class JwtSessionValidator
{
    /// <summary>
    /// Marca la solicitud cuando la sesión no pudo validarse por
    /// un error técnico (por ejemplo, base de datos no disponible),
    /// para responder 503 en lugar de 401.
    /// </summary>
    public const string ValidationErrorItemKey =
        "NovaBooks.SessionValidationError";

    public static async Task OnTokenValidatedAsync(
        TokenValidatedContext context)
    {
        ClaimsPrincipal? principal = context.Principal;

        string? userIdValue =
            principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        string? stampHash =
            principal?.FindFirstValue(CustomClaimTypes.SecurityStamp);

        if (principal is null ||
            !int.TryParse(userIdValue, out int userId) ||
            string.IsNullOrEmpty(stampHash))
        {
            context.Fail("La sesión no es válida.");
            return;
        }

        UserAccessSnapshot? access;

        try
        {
            IUserAccessService userAccessService =
                context.HttpContext.RequestServices
                    .GetRequiredService<IUserAccessService>();

            access = await userAccessService.GetAccessAsync(
                userId,
                context.HttpContext.RequestAborted);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            context.HttpContext.Items[ValidationErrorItemKey] = true;
            context.Fail(exception);
            return;
        }

        if (access is null ||
            !access.User.USU_Activo ||
            access.IsLockedOut ||
            !SecurityStampHasher.Matches(
                access.User.SecurityStamp,
                stampHash))
        {
            context.Fail("La sesión ya no es válida.");
            return;
        }

        ClaimsIdentity identity = new(
            principal.Claims.Where(claim =>
                claim.Type != ClaimTypes.Name &&
                claim.Type != ClaimTypes.Email &&
                claim.Type != ClaimTypes.Role &&
                claim.Type != CustomClaimTypes.Permission),
            principal.Identity?.AuthenticationType,
            ClaimTypes.Name,
            ClaimTypes.Role);

        identity.AddClaim(new Claim(
            ClaimTypes.Name,
            access.User.UserName ?? string.Empty));

        identity.AddClaim(new Claim(
            ClaimTypes.Email,
            access.User.Email ?? string.Empty));

        identity.AddClaims(access.Roles.Select(role =>
            new Claim(ClaimTypes.Role, role)));

        identity.AddClaims(access.Permissions.Select(permission =>
            new Claim(CustomClaimTypes.Permission, permission)));

        context.Principal = new ClaimsPrincipal(identity);
    }

    public static async Task OnChallengeAsync(
        JwtBearerChallengeContext context)
    {
        if (!context.HttpContext.Items.ContainsKey(
                ValidationErrorItemKey))
        {
            return;
        }

        context.HandleResponse();

        IProblemDetailsService problemDetailsService =
            context.HttpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();

        context.Response.StatusCode =
            StatusCodes.Status503ServiceUnavailable;

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails =
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Servicio no disponible.",
                    Detail =
                        "No fue posible validar la sesión. " +
                        "Intente nuevamente en unos segundos.",
                    Extensions =
                    {
                        ["code"] = "service_unavailable"
                    }
                }
            });
    }
}
