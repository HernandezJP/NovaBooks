using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class SupplierPermissions
    {
        public const string View = "Proveedores.Ver";
        public const string Create = "Proveedores.Crear";
        public const string Update = "Proveedores.Modificar";
        public const string Disable = "Proveedores.Desactivar";

        public static readonly string[] All =
        [
            View,
            Create,
            Update,
            Disable
        ];
    }
}
