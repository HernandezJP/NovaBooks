using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class CatalogPermissions
    {
        public const string View = "Catalogo.Ver";
        public const string Create = "Catalogo.Crear";
        public const string Update = "Catalogo.Modificar";
        public const string Disable = "Catalogo.Desactivar";
        public const string ManagePrices = "Catalogo.AdministrarPrecios";

        public static readonly string[] All =
        [
            View,
        Create,
        Update,
        Disable,
        ManagePrices
        ];
    }
}
