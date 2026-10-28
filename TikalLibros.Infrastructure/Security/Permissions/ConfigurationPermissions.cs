namespace TikalLibros.Infrastructure.Security.Permissions;

/// <summary>
/// Parámetros del sistema (descuentos, días de reserva) y estructura de la
/// organización: sucursales, almacenes y cajas.
/// </summary>
public static class ConfigurationPermissions
{
    public const string ManageParameters = "Configuracion.Parametros";
    public const string ManageOrganization = "Configuracion.Organizacion";

    public static readonly string[] All =
    [
        ManageParameters,
        ManageOrganization
    ];
}
