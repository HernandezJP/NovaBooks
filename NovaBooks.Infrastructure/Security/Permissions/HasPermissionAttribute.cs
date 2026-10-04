using Microsoft.AspNetCore.Authorization;

namespace NovaBooks.Infrastructure.Security.Permissions;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException(
                "El permiso es obligatorio.",
                nameof(permission));
        }

        Policy = permission;
    }
}