using NovaBooks.Infrastructure.Data;

namespace NovaBooks.Api.ErrorHandling;

/// <summary>
/// Responde 503 en /api mientras la base de datos se inicializa.
/// </summary>
public sealed class DatabaseReadinessMiddleware
{
    private readonly RequestDelegate _next;

    public DatabaseReadinessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        DatabaseReadiness readiness,
        IProblemDetailsService problemDetailsService)
    {
        if (readiness.IsReady ||
            !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode =
            StatusCodes.Status503ServiceUnavailable;

        context.Response.Headers.RetryAfter = "5";

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails =
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Sistema iniciando.",
                    Detail =
                        "El sistema se está preparando. " +
                        "Intente nuevamente en unos segundos.",
                    Extensions =
                    {
                        ["code"] = "system_starting"
                    }
                }
            });
    }
}
