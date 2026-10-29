namespace TikalLibros.Api.Auditing;

/// <summary>
/// Nombre de la entidad con que se auditan las operaciones del controlador
/// o de una acción (por ejemplo "Cliente" u "OrdenCompra"). En una acción
/// también puede fijar la acción auditada.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class AuditEntityAttribute : Attribute
{
    public AuditEntityAttribute(string entity, string? action = null)
    {
        Entity = entity;
        Action = action;
    }

    public string Entity { get; }

    public string? Action { get; }
}

/// <summary>La acción se audita por su cuenta (por ejemplo el login) o no modifica datos.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class SkipAuditAttribute : Attribute
{
}
