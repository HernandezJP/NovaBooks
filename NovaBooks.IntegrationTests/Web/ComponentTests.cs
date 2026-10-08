extern alias web;

using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using web::NovaBooks.Web.Components.Shared;
using web::NovaBooks.Web.Security;
using web::NovaBooks.Web.Services.Api;

namespace NovaBooks.IntegrationTests.Web;

public sealed class SharedComponentTests : WebTestContext
{
    [Fact]
    public void StatusChip_MuestraElEstado()
    {
        IRenderedComponent<StatusChip> active = Render<StatusChip>(parameters => parameters.Add(p => p.IsActive, true));
        IRenderedComponent<StatusChip> inactive = Render<StatusChip>(parameters => parameters
            .Add(p => p.IsActive, false)
            .Add(p => p.InactiveText, "Desactivado"));

        Assert.Contains("status-pill--active", active.Markup);
        Assert.Equal("Activo", active.Find("span").TextContent.Trim());
        Assert.Equal("Desactivado", inactive.Find("span").TextContent.Trim());
    }

    [Fact]
    public void FormErrors_ConflictoMuestraMensajeYErroresAdicionales()
    {
        ApiResult<bool> result = ApiResult<bool>.Fail(
            ApiFailure.Conflict,
            "El NIT 1234567K ya está registrado.",
            409,
            validationErrors: new Dictionary<string, string[]> { ["Email"] = ["El correo ya está registrado."] });

        IRenderedComponent<FormErrors> component = Render<FormErrors>(parameters => parameters.Add(p => p.Result, result));

        Assert.Contains("conflicto con datos existentes", component.Markup);
        Assert.Contains("El NIT 1234567K ya está registrado.", component.Markup);
        Assert.Contains("El correo ya está registrado.", component.Find("ul").TextContent);
    }

    [Fact]
    public void FormErrors_SinErroresNoMuestraNada()
    {
        IRenderedComponent<FormErrors> component = Render<FormErrors>(parameters => parameters
            .Add(p => p.Result, ApiResult<bool>.Success(true, 200)));

        Assert.Empty(component.FindAll("[role=alert]"));
    }

    [Fact]
    public void FormErrors_CamposInvalidosMuestraAviso()
    {
        IRenderedComponent<FormErrors> component = Render<FormErrors>(parameters => parameters.Add(p => p.ShowInvalidHint, true));

        Assert.Contains("Revise los campos marcados", component.Markup);
    }

    [Fact]
    public void ListPager_MuestraElRangoDeLaPagina()
    {
        IRenderedComponent<ListPager> component = Render<ListPager>(parameters => parameters
            .Add(p => p.Page, 2)
            .Add(p => p.PageSize, 10)
            .Add(p => p.TotalItems, 25)
            .Add(p => p.TotalPages, 3));

        Assert.Contains("Mostrando 11–20 de 25", component.Markup);
    }

    [Fact]
    public void ListPager_SinResultadosNoSeMuestra()
    {
        IRenderedComponent<ListPager> component = Render<ListPager>(parameters => parameters.Add(p => p.TotalItems, 0));

        Assert.Equal(string.Empty, component.Markup.Trim());
    }

    [Theory]
    [InlineData("García Márquez", "garcia", true)]
    [InlineData("Editorial Piedra Santa", "SANTA", true)]
    [InlineData("Rayuela", "cortázar", false)]
    public void SearchText_IgnoraMayusculasYTildes(string text, string search, bool expected)
    {
        Assert.Equal(expected, SearchText.Matches(text, search));
    }
}

/// <summary>
/// Toda página enrutable declara su acceso y los permisos de la Web
/// existen en el catálogo de la API.
/// </summary>
public sealed class PageAccessTests
{
    private static readonly Assembly WebAssembly = typeof(AppPermissions).Assembly;

    // Páginas que solo requieren sesión iniciada.
    private static readonly string[] AuthenticatedOnly = ["/"];

    public static IEnumerable<object[]> RoutablePages() => WebAssembly.GetTypes()
        .Where(type => typeof(IComponent).IsAssignableFrom(type) && type.GetCustomAttributes<RouteAttribute>().Any())
        .Select(type => new object[] { type.FullName! });

    [Theory]
    [MemberData(nameof(RoutablePages))]
    public void PaginaEnrutable_DeclaraPermisoOAccesoAnonimo(string typeName)
    {
        Type page = WebAssembly.GetType(typeName)!;
        bool declaresAccess =
            page.GetCustomAttribute<RequirePermissionAttribute>() is not null ||
            page.GetCustomAttribute<AllowAnonymousPageAttribute>() is not null ||
            page.GetCustomAttributes<RouteAttribute>().All(route => AuthenticatedOnly.Contains(route.Template));

        Assert.True(declaresAccess, $"{typeName} no declara RequirePermission ni AllowAnonymousPage.");
    }

    [Fact]
    public void PermisosDeLaWeb_ExistenEnLaApi()
    {
        HashSet<string> apiPermissions = typeof(NovaBooks.Infrastructure.Security.Permissions.CatalogPermissions).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "NovaBooks.Infrastructure.Security.Permissions")
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet();

        IEnumerable<string> webPermissions = typeof(AppPermissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.Name != nameof(AppPermissions.ClaimType))
            .Select(field => (string)field.GetRawConstantValue()!);

        Assert.All(webPermissions, permission => Assert.Contains(permission, apiPermissions));
    }
}
