using System;
using System.Collections.Generic;
using System.Text;

namespace TikalLibros.Infrastructure.Security.Permissions
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
            .. EmployeePermissions.All,
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
