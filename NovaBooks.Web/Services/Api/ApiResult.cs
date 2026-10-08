namespace NovaBooks.Web.Services.Api;

public enum ApiFailure
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    SystemStarting,
    Unavailable,
    Timeout,
    Network,
    Server
}

/// <summary>
/// Vista no genérica de un resultado, para mostrar errores.
/// </summary>
public interface IApiResultView
{
    bool Succeeded { get; }

    ApiFailure Failure { get; }

    string Message { get; }

    IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
}

/// <summary>
/// Resultado de una llamada a la API. Nunca lanza por errores HTTP o de red;
/// la página decide cómo mostrar Message y ValidationErrors.
/// </summary>
public sealed class ApiResult<T> : IApiResultView
{
    public bool Succeeded { get; private init; }

    public T? Data { get; private init; }

    public ApiFailure Failure { get; private init; }

    public int? StatusCode { get; private init; }

    /// <summary>Código de error de la API (por ejemplo, account_locked).</summary>
    public string? ErrorCode { get; private init; }

    public string Message { get; private init; } = string.Empty;

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; private init; } =
        new Dictionary<string, string[]>();

    public static ApiResult<T> Success(T? data, int statusCode)
    {
        return new ApiResult<T>
        {
            Succeeded = true,
            Data = data,
            StatusCode = statusCode
        };
    }

    public static ApiResult<T> Fail(
        ApiFailure failure,
        string message,
        int? statusCode = null,
        string? errorCode = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        return new ApiResult<T>
        {
            Failure = failure,
            Message = message,
            StatusCode = statusCode,
            ErrorCode = errorCode,
            ValidationErrors = validationErrors ?? new Dictionary<string, string[]>()
        };
    }
}
