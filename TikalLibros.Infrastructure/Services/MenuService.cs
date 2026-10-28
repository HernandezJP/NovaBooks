using TikalLibros.Application.DTOs.Menu;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Infrastructure.Services;

public sealed class MenuService : IMenuService
{
    // Orden del menú: operación diaria primero y configuración al final.
    private static readonly MenuGroupResponse[] Menu =
    [
        Group("inicio", "Inicio",
            Item("inicio", "Inicio", "/", "home", null)),

        Group("ventas", "Ventas",
            Item("punto-venta", "Punto de venta", "/ventas/nueva", "pos",
                SalesPermissions.Create),
            Item("pedidos", "Pedidos", "/pedidos", "orders",
                OrderPermissions.View),
            Item("aprobaciones", "Aprobaciones", "/pedidos/aprobaciones", "approvals",
                OrderPermissions.Approve),
            Item("ventas", "Ventas", "/ventas", "sales",
                SalesPermissions.View),
            Item("devoluciones", "Devoluciones", "/devoluciones", "returns",
                SalesPermissions.Return),
            Item("caja", "Mi caja", "/caja", "cash",
                CashPermissions.Open),
            Item("cierres", "Aperturas y cierres", "/caja/aperturas", "cash-history",
                CashPermissions.View)),

        Group("catalogo", "Catálogo",
            Item("libros", "Libros", "/productos", "books",
                CatalogPermissions.View),
            Item("autores", "Autores", "/autores", "authors",
                CatalogPermissions.View),
            Item("editoriales", "Editoriales", "/editoriales", "publishers",
                CatalogPermissions.View),
            Item("categorias", "Categorías", "/categorias", "categories",
                CatalogPermissions.View),
            Item("listas-precio", "Listas de precio", "/listas-precio", "price-lists",
                CatalogPermissions.View)),

        Group("personas", "Personas",
            Item("clientes", "Clientes", "/clientes", "customers",
                CustomerPermissions.View),
            Item("proveedores", "Proveedores", "/proveedores", "suppliers",
                SupplierPermissions.View),
            Item("empleados", "Empleados", "/empleados", "employees",
                EmployeePermissions.View)),

        Group("compras", "Compras",
            Item("ordenes", "Órdenes de compra", "/compras", "purchases",
                PurchasingPermissions.View),
            Item("recepciones", "Recepciones", "/recepciones", "receptions",
                PurchasingPermissions.View),
            Item("devoluciones-proveedor", "Devoluciones a proveedor", "/compras/devoluciones", "supplier-returns",
                PurchasingPermissions.Return)),

        Group("inventario", "Inventario",
            Item("existencias", "Existencias", "/inventario/existencias", "stock",
                InventoryPermissions.View),
            Item("alertas", "Alertas", "/inventario/alertas", "alerts",
                InventoryPermissions.View),
            Item("movimientos", "Movimientos y kardex", "/inventario/movimientos", "movements",
                InventoryPermissions.ViewMovements),
            Item("ajustes", "Ajustes", "/inventario/ajustes", "adjustments",
                InventoryPermissions.Adjust),
            Item("traslados", "Traslados", "/inventario/traslados", "transfers",
                InventoryPermissions.Transfer)),

        Group("reportes", "Reportes",
            Item("reporte-ventas", "Ventas por período", "/reportes/ventas", "report-sales",
                ReportPermissions.Sales),
            Item("reporte-libros", "Libros más vendidos", "/reportes/libros-mas-vendidos", "report-books",
                ReportPermissions.Sales),
            Item("reporte-inventario", "Inventario valorizado", "/reportes/inventario", "report-inventory",
                ReportPermissions.Inventory),
            Item("reporte-pedidos", "Pedidos pendientes", "/reportes/pedidos-pendientes", "report-orders",
                ReportPermissions.Sales),
            Item("reporte-cobrar", "Cuentas por cobrar", "/reportes/cuentas-por-cobrar", "report-receivables",
                ReportPermissions.Sales),
            Item("reporte-rentabilidad", "Rentabilidad", "/reportes/rentabilidad", "report-profit",
                ReportPermissions.Profitability),
            Item("reporte-compras", "Compras", "/reportes/compras", "report-purchases",
                ReportPermissions.Purchases),
            Item("reporte-caja", "Caja", "/reportes/caja", "report-cash",
                ReportPermissions.Cash),
            Item("auditoria", "Auditoría", "/auditoria", "audit",
                ReportPermissions.Audit)),

        Group("configuracion", "Configuración",
            Item("usuarios", "Usuarios", "/usuarios", "users",
                UserPermissions.View),
            Item("roles", "Roles", "/roles", "roles",
                RolePermissions.View),
            Item("permisos", "Permisos", "/permisos", "permissions",
                RolePermissions.AssignPermissions),
            Item("sucursales", "Sucursales", "/configuracion/sucursales", "branches",
                ConfigurationPermissions.ManageOrganization),
            Item("almacenes", "Almacenes", "/configuracion/almacenes", "warehouses",
                ConfigurationPermissions.ManageOrganization),
            Item("cajas", "Cajas", "/configuracion/cajas", "cash-registers",
                ConfigurationPermissions.ManageOrganization),
            Item("parametros", "Parámetros", "/configuracion/parametros", "parameters",
                ConfigurationPermissions.ManageParameters))
    ];

    public IReadOnlyCollection<MenuGroupResponse> GetMenu(
        IEnumerable<string> permissions)
    {
        HashSet<string> granted = new(
            permissions,
            StringComparer.OrdinalIgnoreCase);

        return Menu
            .Select(group => new MenuGroupResponse
            {
                Key = group.Key,
                Title = group.Title,
                Items = group.Items
                    .Where(item =>
                        item.RequiredPermission is null ||
                        granted.Contains(item.RequiredPermission))
                    .ToArray()
            })
            .Where(group => group.Items.Count > 0)
            .ToArray();
    }

    private static MenuGroupResponse Group(
        string key,
        string title,
        params MenuItemResponse[] items)
    {
        return new MenuGroupResponse
        {
            Key = key,
            Title = title,
            Items = items
        };
    }

    private static MenuItemResponse Item(
        string key,
        string title,
        string href,
        string icon,
        string? requiredPermission)
    {
        return new MenuItemResponse
        {
            Key = key,
            Title = title,
            Href = href,
            Icon = icon,
            RequiredPermission = requiredPermission
        };
    }
}
