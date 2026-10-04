using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class CustomerPermissions
    {
        public const string View = "Clientes.Ver";
        public const string Create = "Clientes.Crear";
        public const string Update = "Clientes.Modificar";
        public const string Disable = "Clientes.Desactivar";

        public static readonly string[] All =
        [
            View,
            Create,
            Update,
            Disable 
        ];
    }
}
