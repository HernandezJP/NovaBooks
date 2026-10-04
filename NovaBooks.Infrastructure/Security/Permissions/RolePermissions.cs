using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class RolePermissions
    {
        public const string View = "Roles.Ver";
        public const string Create = "Roles.Crear";
        public const string Update = "Roles.Modificar";
        public const string Delete = "Roles.Eliminar";
        public const string AssignPermissions = "Roles.AsignarPermisos";

        public static readonly string[] All =
        [
            View,
            Create,
            Update,
            Delete,
            AssignPermissions
        ];
    }
}
