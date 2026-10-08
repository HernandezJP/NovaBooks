using NovaBooks.Application.DTOs.Menu;
using NovaBooks.Infrastructure.Security.Permissions;
using NovaBooks.Infrastructure.Services;
using NovaBooks.Infrastructure.Services.Authentication;

namespace NovaBooks.UnitTests.Security;

public sealed class SecurityStampHasherTests
{
    [Fact]
    public void Matches_EsVerdaderoSoloParaElMismoStamp()
    {
        string hash = SecurityStampHasher.Hash("STAMP-1");

        Assert.True(SecurityStampHasher.Matches("STAMP-1", hash));
        Assert.False(SecurityStampHasher.Matches("STAMP-2", hash));
    }

    [Fact]
    public void Hash_NoContieneElStampOriginal()
    {
        Assert.DoesNotContain("STAMP-1", SecurityStampHasher.Hash("STAMP-1"));
    }

    [Theory]
    [InlineData(null, "ABC")]
    [InlineData("STAMP", null)]
    [InlineData("", "")]
    public void Matches_ConValoresVacios_EsFalso(string? stamp, string? hash)
    {
        Assert.False(SecurityStampHasher.Matches(stamp, hash));
    }
}

public sealed class PermissionRequirementTests
{
    [Fact]
    public void Constructor_SeparaVariosPermisosPorBarra()
    {
        PermissionRequirement requirement = new("Roles.Ver|Roles.AsignarPermisos");

        Assert.Equal(["Roles.Ver", "Roles.AsignarPermisos"], requirement.Permissions);
    }

    [Fact]
    public void HasPermissionAttribute_UsaElPrefijoDelProveedorDinamico()
    {
        HasPermissionAttribute attribute = new(UserPermissions.View);

        Assert.Equal(PermissionPolicyProvider.PolicyPrefix + "Usuarios.Ver", attribute.Policy);
    }

    [Fact]
    public void SystemPermissions_NoTieneDuplicados()
    {
        IReadOnlyCollection<string> all = SystemPermissions.GetAll();

        Assert.Equal(all.Count, all.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}

public sealed class MenuServiceTests
{
    private readonly MenuService _service = new();

    [Fact]
    public void GetMenu_SinPermisos_SoloMuestraInicio()
    {
        IReadOnlyCollection<MenuGroupResponse> menu = _service.GetMenu([]);

        MenuGroupResponse group = Assert.Single(menu);
        Assert.Equal("inicio", group.Key);
    }

    [Fact]
    public void GetMenu_MuestraSoloOpcionesAutorizadas()
    {
        IReadOnlyCollection<MenuGroupResponse> menu =
            _service.GetMenu([CustomerPermissions.View, UserPermissions.View]);

        string[] items = menu.SelectMany(group => group.Items).Select(item => item.Key).ToArray();

        Assert.Equal(["inicio", "clientes", "usuarios"], items);
    }

    [Fact]
    public void GetMenu_OcultaGruposSinOpciones()
    {
        IReadOnlyCollection<MenuGroupResponse> menu = _service.GetMenu([CatalogPermissions.View]);

        Assert.DoesNotContain(menu, group => group.Key == "configuracion");
        Assert.Contains(menu, group => group.Key == "catalogo");
    }

    [Fact]
    public void GetMenu_PermisosAsignarPermisosMuestraPaginaPermisos()
    {
        IReadOnlyCollection<MenuGroupResponse> menu = _service.GetMenu([RolePermissions.AssignPermissions]);

        Assert.Contains(menu.SelectMany(group => group.Items), item => item.Href == "/permisos");
    }
}
