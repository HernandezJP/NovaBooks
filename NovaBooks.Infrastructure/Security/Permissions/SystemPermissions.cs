using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public static class SystemPermissions
    {
        public static IReadOnlyCollection<string> GetAll()
        {
            return
            [
                .. UserPermissions.All,
            .. RolePermissions.All,
            .. CustomerPermissions.All,
            .. SupplierPermissions.All,
            .. CatalogPermissions.All,
            .. PurchasingPermissions.All,
            .. InventoryPermissions.All,
            .. SalesPermissions.All,
            .. CashPermissions.All,
            .. ReportPermissions.All
            ];
        }
    }
}
