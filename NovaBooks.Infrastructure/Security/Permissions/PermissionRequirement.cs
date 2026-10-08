using Microsoft.AspNetCore.Authorization;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    /// <summary>
    /// Se cumple si el usuario tiene al menos uno de los permisos.
    /// En el nombre de la política se separan con '|'.
    /// </summary>
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public const char Separator = '|';

        public PermissionRequirement(string permission)
        {
            Permission = permission;

            Permissions = permission.Split(
                Separator,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
        }

        public string Permission { get; }

        public IReadOnlyCollection<string> Permissions { get; }
    }
}
