using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class InventoryPermissions
    {
        public const string View = "Inventario.Ver";
        public const string ViewCosts = "Inventario.VerCostos";
        public const string Transfer = "Inventario.Transferir";
        public const string Adjust = "Inventario.Ajustar";
        public const string ViewMovements = "Inventario.VerMovimientos";

        public static readonly string[] All =
        [
            View,
            ViewCosts,
            Transfer,
            Adjust,
            ViewMovements
        ];
    }
}
