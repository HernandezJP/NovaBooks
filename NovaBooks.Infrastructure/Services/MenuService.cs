using NovaBooks.Application.DTOs.Menu;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Infrastructure.Services;

public sealed class MenuService : IMenuService
{
    // Orden del menú: operación diaria primero y configuración al final.
    private static readonly MenuGroupResponse[] Menu =
    [
        Group("inicio", "Inicio",
            Item("inicio", "Inicio", "/", "home", null)),

        Group("catalogo", "Catálogo",
            Item("libros", "Libros", "/productos", "books",
                CatalogPermissions.View),
            Item("autores", "Autores", "/autores", "authors",
                CatalogPermissions.View),
            Item("editoriales", "Editoriales", "/editoriales", "publishers",
                CatalogPermissions.View)),

        Group("personas", "Personas",
            Item("clientes", "Clientes", "/clientes", "customers",
                CustomerPermissions.View)),

        Group("configuracion", "Configuración",
            Item("usuarios", "Usuarios", "/usuarios", "users",
                UserPermissions.View),
            Item("roles", "Roles", "/roles", "roles",
                RolePermissions.View),
            Item("permisos", "Permisos", "/permisos", "permissions",
                RolePermissions.AssignPermissions))
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
