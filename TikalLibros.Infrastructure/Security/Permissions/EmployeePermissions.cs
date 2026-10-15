using System;
using System.Collections.Generic;
using System.Text;

namespace TikalLibros.Infrastructure.Security.Permissions
{
    public static class EmployeePermissions
    {
        public const string View = "Empleados.Ver";
        public const string Create = "Empleados.Crear";
        public const string Update = "Empleados.Modificar";
        public const string Disable = "Empleados.Desactivar";

        public static readonly string[] All =
        [
            View,
            Create,
            Update,
            Disable
        ];
    }
}
