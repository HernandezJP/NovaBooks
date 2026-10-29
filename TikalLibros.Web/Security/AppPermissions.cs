namespace TikalLibros.Web.Security;

/// <summary>
/// Permisos usados por la Web. Deben coincidir con los de TikalLibros.Api;
/// la API vuelve a validarlos en cada solicitud.
/// </summary>
public static class AppPermissions
{
    public const string ClaimType = "permission";

    public const string UsersView = "Usuarios.Ver";
    public const string UsersCreate = "Usuarios.Crear";
    public const string UsersUpdate = "Usuarios.Modificar";
    public const string UsersDisable = "Usuarios.Desactivar";
    public const string UsersAssignRoles = "Usuarios.AsignarRoles";
    public const string UsersResetPassword = "Usuarios.RestablecerPassword";

    public const string RolesView = "Roles.Ver";
    public const string RolesCreate = "Roles.Crear";
    public const string RolesUpdate = "Roles.Modificar";
    public const string RolesDelete = "Roles.Eliminar";
    public const string RolesAssignPermissions = "Roles.AsignarPermisos";

    public const string CustomersView = "Clientes.Ver";
    public const string CustomersCreate = "Clientes.Crear";
    public const string CustomersUpdate = "Clientes.Modificar";
    public const string CustomersDisable = "Clientes.Desactivar";

    public const string CatalogView = "Catalogo.Ver";
    public const string CatalogCreate = "Catalogo.Crear";
    public const string CatalogUpdate = "Catalogo.Modificar";
    public const string CatalogDisable = "Catalogo.Desactivar";
    public const string CatalogManagePrices = "Catalogo.AdministrarPrecios";

    public const string EmployeesView = "Empleados.Ver";
    public const string EmployeesCreate = "Empleados.Crear";
    public const string EmployeesUpdate = "Empleados.Modificar";
    public const string EmployeesDisable = "Empleados.Desactivar";

    public const string SuppliersView = "Proveedores.Ver";
    public const string SuppliersCreate = "Proveedores.Crear";
    public const string SuppliersUpdate = "Proveedores.Modificar";
    public const string SuppliersDisable = "Proveedores.Desactivar";

    public const string PurchasesView = "Compras.Ver";
    public const string PurchasesCreate = "Compras.Crear";
    public const string PurchasesUpdate = "Compras.Modificar";
    public const string PurchasesApprove = "Compras.Aprobar";
    public const string PurchasesReceive = "Compras.Recibir";
    public const string PurchasesCancel = "Compras.Cancelar";
    public const string PurchasesReturn = "Compras.Devolver";

    public const string InventoryView = "Inventario.Ver";
    public const string InventoryViewCosts = "Inventario.VerCostos";
    public const string InventoryAdjust = "Inventario.Ajustar";
    public const string InventoryTransfer = "Inventario.Transferir";
    public const string InventoryViewMovements = "Inventario.VerMovimientos";

    public const string OrdersView = "Pedidos.Ver";
    public const string OrdersCreate = "Pedidos.Crear";
    public const string OrdersUpdate = "Pedidos.Modificar";
    public const string OrdersApprove = "Pedidos.Aprobar";
    public const string OrdersCancel = "Pedidos.Cancelar";

    public const string ConfigurationParameters = "Configuracion.Parametros";
    public const string ConfigurationOrganization = "Configuracion.Organizacion";

    public const string SalesView = "Ventas.Ver";
    public const string SalesCreate = "Ventas.Crear";
    public const string SalesCancel = "Ventas.Anular";
    public const string SalesApplyDiscount = "Ventas.AplicarDescuento";
    public const string SalesSellOnCredit = "Ventas.VenderCredito";
    public const string SalesReturn = "Ventas.Devolver";

    public const string CashView = "Caja.Ver";
    public const string CashOpen = "Caja.Abrir";
    public const string CashClose = "Caja.Cerrar";
    public const string CashCreateIncome = "Caja.RegistrarIngreso";
    public const string CashCreateExpense = "Caja.RegistrarEgreso";
    public const string CashCancelMovement = "Caja.AnularMovimiento";

    public const string ReportsSales = "Reportes.Ventas";
    public const string ReportsInventory = "Reportes.Inventario";
    public const string ReportsPurchases = "Reportes.Compras";
    public const string ReportsCash = "Reportes.Caja";
    public const string ReportsProfitability = "Reportes.Rentabilidad";
    public const string ReportsAudit = "Reportes.Auditoria";
}
