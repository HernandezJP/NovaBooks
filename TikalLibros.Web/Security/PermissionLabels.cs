using System.Text.RegularExpressions;

namespace TikalLibros.Web.Security;

/// <summary>
/// Convierte códigos de permiso (por ejemplo, "AsignarPermisos") en texto
/// legible ("Asignar permisos") solo para mostrar.
/// </summary>
public static partial class PermissionLabels
{
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Catalogo"] = "Catálogo",
        ["Auditoria"] = "Auditoría",
        ["Configuracion"] = "Configuración",
        ["Parametros"] = "Parámetros del sistema",
        ["Organizacion"] = "Sucursales, almacenes y cajas",
        ["RestablecerPassword"] = "Restablecer contraseña",
        ["VenderCredito"] = "Vender al crédito",
        ["AdministrarPrecios"] = "Administrar precios"
    };

    public static string Humanize(string code)
    {
        if (Known.TryGetValue(code, out string? label))
        {
            return label;
        }

        string[] words = WordBoundary().Split(code);

        return string.Join(" ", words.Select((word, index) =>
            index == 0 ? word : word.ToLowerInvariant()));
    }

    [GeneratedRegex("(?<=[a-záéíóúñ])(?=[A-ZÁÉÍÓÚÑ])")]
    private static partial Regex WordBoundary();
}
