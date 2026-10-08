using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NovaBooks.Application.Common;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Api.Common;

public static class ControllerExtensions
{
    /// <summary>
    /// Convierte un resultado fallido en ProblemDetails
    /// (400 validación, 404 inexistente, 409 conflicto).
    /// </summary>
    public static ObjectResult OperationProblem<T>(
        this ControllerBase controller,
        OperationResult<T> result)
    {
        (int status, string title) = result.ErrorType switch
        {
            OperationErrorType.NotFound => (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado."),

            OperationErrorType.Conflict => (
                StatusCodes.Status409Conflict,
                "Conflicto de negocio."),

            _ => (
                StatusCodes.Status400BadRequest,
                "Solicitud no válida.")
        };

        ObjectResult problem = controller.Problem(
            detail: string.Join(" ", result.Errors),
            statusCode: status,
            title: title);

        if (problem.Value is ProblemDetails details)
        {
            details.Extensions["errors"] = result.Errors;
        }

        return problem;
    }

    public static ObjectResult NotFoundProblem(
        this ControllerBase controller,
        string detail)
    {
        return controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            title: "Recurso no encontrado.");
    }

    public static ObjectResult ForbiddenProblem(
        this ControllerBase controller,
        string detail)
    {
        return controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status403Forbidden,
            title: "Acceso denegado.");
    }

    public static bool HasPermission(
        this ControllerBase controller,
        string permission)
    {
        return controller.User.HasClaim(
            CustomClaimTypes.Permission,
            permission);
    }

    /// <summary>
    /// La autenticación ya validó el token; un id ausente indica
    /// un token mal formado y se trata como error interno.
    /// </summary>
    public static int GetAuthenticatedUserId(
        this ControllerBase controller)
    {
        string? value =
            controller.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int userId)
            ? userId
            : throw new InvalidOperationException(
                "El token no contiene el identificador del usuario.");
    }
}
