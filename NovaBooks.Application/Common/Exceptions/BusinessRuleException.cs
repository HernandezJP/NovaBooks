namespace NovaBooks.Application.Common.Exceptions;

/// <summary>
/// Conflicto con una regla de negocio. La API lo traduce a 409.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
