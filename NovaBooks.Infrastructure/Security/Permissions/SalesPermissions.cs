using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class SalesPermissions
    {
        public const string View = "Ventas.Ver";
        public const string Create = "Ventas.Crear";
        public const string Cancel = "Ventas.Anular";
        public const string ApplyDiscount = "Ventas.AplicarDescuento";
        public const string SellOnCredit = "Ventas.VenderCredito";
        public const string Return = "Ventas.Devolver";

        public static readonly string[] All =
        [
            View,
            Create,
            Cancel,
            ApplyDiscount,
            SellOnCredit,
            Return
        ];
    }
}
