namespace NovaBooks.Application.Common;

public sealed class UserOperationResult<T>
{
    public bool Succeeded { get; private init; }

    public bool NotFound { get; private init; }

    public T? Data { get; private init; }

    public IReadOnlyCollection<string> Errors { get; private init; } =
        Array.Empty<string>();

    public static UserOperationResult<T> Success(T data)
    {
        return new UserOperationResult<T>
        {
            Succeeded = true,
            Data = data
        };
    }

    public static UserOperationResult<T> Failure(
        params string[] errors)
    {
        return new UserOperationResult<T>
        {
            Errors = errors
        };
    }

    public static UserOperationResult<T> Missing(
        string message)
    {
        return new UserOperationResult<T>
        {
            NotFound = true,
            Errors = [message]
        };
    }
}