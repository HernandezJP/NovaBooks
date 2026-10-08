namespace NovaBooks.Web.Security;

/// <summary>
/// Permisos usados por la Web. Deben coincidir con los de NovaBooks.Api;
/// la API vuelve a validarlos en cada solicitud.
/// </summary>
public static class AppPermissions
{
    public const string ClaimType = "permission";

    public const string UsersView = "Usuarios.Ver";
    public const string UsersCreate = "Usuarios.Crear";
    public const string UsersUpdate = "Usuarios.Modificar";
    public const string UsersDisable = "Usuarios.Desactivar";
    public const string UsersAssignRoles = "Usuarios.AsignarRoles";
    public const string UsersResetPassword = "Usuarios.RestablecerPassword";

    public const string RolesView = "Roles.Ver";
    public const string RolesCreate = "Roles.Crear";
    public const string RolesUpdate = "Roles.Modificar";
    public const string RolesDelete = "Roles.Eliminar";
    public const string RolesAssignPermissions = "Roles.AsignarPermisos";

    public const string CustomersView = "Clientes.Ver";
    public const string CustomersCreate = "Clientes.Crear";
    public const string CustomersUpdate = "Clientes.Modificar";
    public const string CustomersDisable = "Clientes.Desactivar";

    public const string CatalogView = "Catalogo.Ver";
    public const string CatalogCreate = "Catalogo.Crear";
    public const string CatalogUpdate = "Catalogo.Modificar";
    public const string CatalogDisable = "Catalogo.Desactivar";
    public const string CatalogManagePrices = "Catalogo.AdministrarPrecios";
}
