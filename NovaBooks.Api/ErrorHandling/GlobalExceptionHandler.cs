using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using NovaBooks.Application.Common.Exceptions;

namespace NovaBooks.Api.ErrorHandling;

/// <summary>
/// Traduce excepciones a ProblemDetails sin exponer
/// detalles internos ni stack traces.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    private readonly IProblemDetailsService _problemDetailsService;

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        (int status, string title, string detail, string code) =
            exception switch
            {
                NotFoundException => (
                    StatusCodes.Status404NotFound,
                    "Recurso no encontrado.",
                    exception.Message,
                    "not_found"),

                BusinessRuleException => (
                    StatusCodes.Status409Conflict,
                    "Conflicto de negocio.",
                    exception.Message,
                    "conflict"),

                _ when IsDatabaseUnavailable(exception) => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Servicio no disponible.",
                    "No fue posible acceder a la base de datos. " +
                    "Intente nuevamente en unos segundos.",
                    "database_unavailable"),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Error interno.",
                    "Ocurrió un error inesperado. Intente nuevamente " +
                    "o comuníquese con un administrador.",
                    "internal_error")
            };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Error no controlado en {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                    Extensions =
                    {
                        ["code"] = code
                    }
                }
            });
    }

    private static bool IsDatabaseUnavailable(Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is DbException or
                RetryLimitExceededException or
                TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
