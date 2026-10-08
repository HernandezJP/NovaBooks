namespace NovaBooks.Application.Common;

public enum OperationErrorType
{
    None,
    Validation,
    NotFound,
    Conflict
}

/// <summary>
/// Resultado de una operación de negocio. La API traduce
/// Validation a 400, NotFound a 404 y Conflict a 409.
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
