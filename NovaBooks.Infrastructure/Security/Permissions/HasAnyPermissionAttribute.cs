using Microsoft.AspNetCore.Authorization;

namespace NovaBooks.Infrastructure.Security.Permissions;

/// <summary>
/// Autoriza si el usuario tiene cualquiera de los permisos indicados.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class HasAnyPermissionAttribute : AuthorizeAttribute
{
    public HasAnyPermissionAttribute(params string[] permissions)
    {
        if (permissions.Length == 0 ||
            permissions.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Debe indicar al menos un permiso válido.",
                nameof(permissions));
        }

        Policy = PermissionPolicyProvider.PolicyPrefix +
                 string.Join(PermissionRequirement.Separator, permissions);
    }
}
