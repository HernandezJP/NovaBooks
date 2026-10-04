using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class UserPermissions
    {
        public const string View = "Usuarios.Ver";
        public const string Create = "Usuarios.Crear";
        public const string Update = "Usuarios.Modificar";
        public const string Disable = "Usuarios.Desactivar";
        public const string AssignRoles = "Usuarios.AsignarRoles";
        public const string ResetPassword = "Usuarios.RestablecerPassword";

        public static readonly string[] All =
        [
            View,
            Create,
            Update,
            Disable,
            AssignRoles,
            ResetPassword
        ];
    }
}
