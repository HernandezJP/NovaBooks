namespace TikalLibros.Application.Common;

public enum OperationErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

/// <summary>
/// Resultado de una operación de negocio. La API traduce
/// Validation a 400, Forbidden a 403, NotFound a 404 y Conflict a 409.
/// </summary>
public sealed class OperationResult<T>
{
    public bool Succeeded { get; private init; }

    public OperationErrorType ErrorType { get; private init; }

    public bool NotFound =>
        ErrorType == OperationErrorType.NotFound;

    public T? Data { get; private init; }

    public IReadOnlyCollection<string> Errors { get; private init; } =
        Array.Empty<string>();

    /// <summary>El mismo error con otro tipo de dato (para propagar fallos entre operaciones).</summary>
    public OperationResult<TOther> As<TOther>()
    {
        if (Succeeded)
        {
            throw new InvalidOperationException("Solo un resultado fallido puede convertirse.");
        }

        return ErrorType switch
        {
            OperationErrorType.NotFound => OperationResult<TOther>.Missing(Errors.First()),
            OperationErrorType.Conflict => OperationResult<TOther>.Conflict(Errors.ToArray()),
            OperationErrorType.Forbidden => OperationResult<TOther>.Forbidden(Errors.First()),
            _ => OperationResult<TOther>.Failure(Errors.ToArray())
        };
    }

    public static OperationResult<T> Success(T data)
    {
        return new OperationResult<T>
        {
            Succeeded = true,
            Data = data
        };
    }

    public static OperationResult<T> Failure(
        params string[] errors)
    {
        return new OperationResult<T>
        {
            ErrorType = OperationErrorType.Validation,
            Errors = errors
        };
    }

    public static OperationResult<T> Conflict(
        params string[] errors)
    {
        return new OperationResult<T>
        {
            ErrorType = OperationErrorType.Conflict,
            Errors = errors
        };
    }

    /// <summary>El usuario no tiene el permiso que esta operación concreta exige.</summary>
    public static OperationResult<T> Forbidden(
        string message)
    {
        return new OperationResult<T>
        {
            ErrorType = OperationErrorType.Forbidden,
            Errors = [message]
        };
    }

    public static OperationResult<T> Missing(
        string message)
    {
        return new OperationResult<T>
        {
            ErrorType = OperationErrorType.NotFound,
            Errors = [message]
        };
    }
}
