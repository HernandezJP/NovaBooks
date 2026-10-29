using TikalLibros.Application.DTOs.Menu;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.Infrastructure.Services;

namespace TikalLibros.UnitTests.Security;

/// <summary>Menú de compras, inventario, proveedores y empleados.</summary>
public sealed class OperationsMenuTests
{
    private readonly MenuService _service = new();

    private string[] Items(params string[] permissions) =>
        _service.GetMenu(permissions).SelectMany(group => group.Items).Select(item => item.Key).ToArray();

    [Fact]
    public void Compras_SoloConPermisoDeVerCompras()
    {
        Assert.DoesNotContain("ordenes", Items(CustomerPermissions.View));
        Assert.Equal(["inicio", "ordenes", "recepciones"], Items(PurchasingPermissions.View));
    }

    [Fact]
    public void Inventario_CadaOpcionConSuPermiso()
    {
        Assert.Equal(["inicio", "existencias", "alertas"], Items(InventoryPermissions.View));
        Assert.Equal(["inicio", "movimientos"], Items(InventoryPermissions.ViewMovements));
        Assert.Equal(["inicio", "ajustes"], Items(InventoryPermissions.Adjust));
    }

    [Fact]
    public void Personas_IncluyeProveedoresYEmpleadosSegunPermisos()
    {
        IReadOnlyCollection<MenuGroupResponse> menu = _service.GetMenu([SupplierPermissions.View, EmployeePermissions.View]);

        MenuGroupResponse people = Assert.Single(menu, group => group.Key == "personas");
        Assert.Equal(["proveedores", "empleados"], people.Items.Select(item => item.Key).ToArray());
    }

    [Fact]
    public void Administrador_VeTodosLosGruposEnOrden()
    {
        string[] groups = _service.GetMenu(SystemPermissions.GetAll()).Select(group => group.Key).ToArray();

        Assert.Equal(["inicio", "ventas", "catalogo", "personas", "compras", "inventario", "reportes", "configuracion"], groups);
    }

    [Fact]
    public void CadaOpcion_TieneIconoYRutaAbsoluta()
    {
        MenuItemResponse[] items = _service.GetMenu(SystemPermissions.GetAll()).SelectMany(group => group.Items).ToArray();

        Assert.All(items, item =>
        {
            Assert.StartsWith("/", item.Href);
            Assert.False(string.IsNullOrWhiteSpace(item.Icon));
        });
        Assert.Equal(items.Length, items.Select(item => item.Href).Distinct().Count());
    }
}
