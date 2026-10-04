using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class CashPermissions
    {
        public const string Open = "Caja.Abrir";
        public const string Close = "Caja.Cerrar";
        public const string View = "Caja.Ver";
        public const string CreateIncome = "Caja.RegistrarIngreso";
        public const string CreateExpense = "Caja.RegistrarEgreso";
        public const string CancelMovement = "Caja.AnularMovimiento";

        public static readonly string[] All =
        [
            Open,
            Close,
            View,
            CreateIncome,
            CreateExpense,
            CancelMovement
        ];
    }
}
