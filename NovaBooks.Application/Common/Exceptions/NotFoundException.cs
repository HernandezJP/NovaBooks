namespace NovaBooks.Application.Common.Exceptions;

/// <summary>
/// Recurso inexistente. La API lo traduce a 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
