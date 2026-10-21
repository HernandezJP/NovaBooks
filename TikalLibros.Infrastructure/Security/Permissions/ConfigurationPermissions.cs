namespace TikalLibros.Infrastructure.Security.Permissions;

/// <summary>Parámetros del sistema (descuentos, días de reserva).</summary>
public static class ConfigurationPermissions
{
    public const string ManageParameters = "Configuracion.Parametros";

    public static readonly string[] All =
    [
        ManageParameters
    ];
}
