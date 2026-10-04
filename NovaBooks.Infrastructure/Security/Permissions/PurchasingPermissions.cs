using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class PurchasingPermissions
    {
        public const string View = "Compras.Ver";
        public const string Create = "Compras.Crear";
        public const string Update = "Compras.Modificar";
        public const string Approve = "Compras.Aprobar";
        public const string Receive = "Compras.Recibir";
        public const string Cancel = "Compras.Cancelar";

        public static readonly string[] All =
        [
            View,
            Create,
            Update,
            Approve,
            Receive,
            Cancel
        ];
    }
}
