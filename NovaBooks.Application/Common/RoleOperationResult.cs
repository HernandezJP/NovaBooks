namespace NovaBooks.Application.Common;

public sealed class RoleOperationResult<T>
{
    public bool Succeeded { get; private init; }

    public bool NotFound { get; private init; }

    public T? Data { get; private init; }

    public IReadOnlyCollection<string> Errors { get; private init; } =
        Array.Empty<string>();

    public static RoleOperationResult<T> Success(T data)
    {
        return new RoleOperationResult<T>
        {
            Succeeded = true,
            Data = data
        };
    }

    public static RoleOperationResult<T> Failure(
        params string[] errors)
    {
        return new RoleOperationResult<T>
        {
            Succeeded = false,
            Errors = errors
        };
    }

    public static RoleOperationResult<T> Missing(
        string message)
    {
        return new RoleOperationResult<T>
        {
            Succeeded = false,
            NotFound = true,
            Errors = [message]
        };
    }
}