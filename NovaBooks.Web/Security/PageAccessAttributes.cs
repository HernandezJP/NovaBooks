namespace NovaBooks.Web.Security;

/// <summary>
/// La página exige el permiso indicado. AppRouteView lo valida antes de
/// renderizarla, incluso al entrar por URL directa.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

/// <summary>
/// La página no requiere sesión (login, errores). Las demás sí.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class AllowAnonymousPageAttribute : Attribute;
