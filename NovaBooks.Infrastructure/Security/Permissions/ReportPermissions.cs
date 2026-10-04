using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class ReportPermissions
    {
        public const string Sales = "Reportes.Ventas";
        public const string Purchases = "Reportes.Compras";
        public const string Inventory = "Reportes.Inventario";
        public const string Cash = "Reportes.Caja";
        public const string Profitability = "Reportes.Rentabilidad";
        public const string Audit = "Reportes.Auditoria";

        public static readonly string[] All =
        [
            Sales,
            Purchases,
            Inventory,
            Cash,
            Profitability,
            Audit
        ];
    }
}
