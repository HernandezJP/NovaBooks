extern alias web;

using System.Reflection;
using Microsoft.AspNetCore.Components;
using TikalLibros.Application.DTOs.Menu;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.Infrastructure.Services;
using web::TikalLibros.Web.Security;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>
/// El menú que entrega la API apunta a páginas que existen en la Web y que
/// exigen el mismo permiso con el que se muestra la opción.
/// </summary>
public sealed class MenuRoutesTests
{
    private static readonly Dictionary<string, Type> Pages = typeof(AppPermissions).Assembly.GetTypes()
        .Where(type => typeof(IComponent).IsAssignableFrom(type))
        .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => (route.Template, type)))
        .ToDictionary(item => item.Template, item => item.type, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<object[]> MenuItems() => new MenuService()
        .GetMenu(SystemPermissions.GetAll())
        .SelectMany(group => group.Items)
        .Select(item => new object[] { item.Href, item.RequiredPermission ?? string.Empty });

    [Theory]
    [MemberData(nameof(MenuItems))]
    public void OpcionDelMenu_TienePaginaConElMismoPermiso(string href, string permission)
    {
        Assert.True(Pages.TryGetValue(href, out Type? page), $"No existe una página con la ruta {href}.");

        string? required = page!.GetCustomAttribute<RequirePermissionAttribute>()?.Permission;

        if (permission.Length > 0)
        {
            Assert.Equal(permission, required);
        }
    }
}
