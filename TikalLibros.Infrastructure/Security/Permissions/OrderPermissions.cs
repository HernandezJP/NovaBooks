namespace TikalLibros.Infrastructure.Security.Permissions;

/// <summary>Pedidos de clientes: registro, aprobación y cancelación.</summary>
public static class OrderPermissions
{
    public const string View = "Pedidos.Ver";
    public const string Create = "Pedidos.Crear";
    public const string Update = "Pedidos.Modificar";

    /// <summary>Aprueba o rechaza pedidos con descuento alto o que exceden el crédito.</summary>
    public const string Approve = "Pedidos.Aprobar";

    public const string Cancel = "Pedidos.Cancelar";

    public static readonly string[] All =
    [
        View,
        Create,
        Update,
        Approve,
        Cancel
    ];
}
