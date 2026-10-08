using System.Net;
using System.Net.Http.Json;
using NovaBooks.Application.DTOs.Menu;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.DTOs.Users;
using NovaBooks.IntegrationTests.Infrastructure;

namespace NovaBooks.IntegrationTests.Api;

/// <summary>
/// Permisos por endpoint, menú filtrado e invalidación de sesiones: los
/// cambios en usuarios, roles y permisos aplican al token ya emitido.
/// </summary>
public sealed class AuthorizationTests : ApiTestBase
{
    public AuthorizationTests(NovaBooksApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Endpoint_SinElPermisoRequerido_Devuelve403()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/clientes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/roles", new CreateRoleRequest { Name = Unique("Rol") })).StatusCode);
    }

    [Fact]
    public async Task Menu_SoloIncluyeLasOpcionesPermitidas()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);

        List<MenuGroupResponse>? menu = await client.GetFromJsonAsync<List<MenuGroupResponse>>("api/menu");
        List<string> hrefs = menu!.SelectMany(group => group.Items).Select(item => item.Href).ToList();

        Assert.Contains(hrefs, href => href.Contains("clientes"));
        Assert.DoesNotContain(menu, group => group.Key == "configuracion");
        Assert.DoesNotContain(hrefs, href => href.Contains("usuarios"));
    }

    [Fact]
    public async Task QuitarPermisoAlRol_AplicaAlTokenVigente()
    {
        HttpClient admin = await AdminAsync();
        (string userName, int userId) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/clientes")).StatusCode);

        UserResponse? user = await admin.GetFromJsonAsync<UserResponse>($"api/usuarios/{userId}");
        RoleResponse role = (await admin.GetFromJsonAsync<List<RoleResponse>>("api/roles"))!
            .Single(item => item.Name == user!.Roles.Single());

        HttpResponseMessage update = await admin.PutAsJsonAsync(
            $"api/roles/{role.Id}/permisos",
            new AssignRolePermissionsRequest { Permissions = ["Catalogo.Ver"] });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/clientes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/productos")).StatusCode);
    }

    [Fact]
    public async Task DesactivarRol_RetiraSusPermisosAlUsuario()
    {
        HttpClient admin = await AdminAsync();
        (string userName, int userId) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);

        UserResponse? user = await admin.GetFromJsonAsync<UserResponse>($"api/usuarios/{userId}");
        RoleResponse role = (await admin.GetFromJsonAsync<List<RoleResponse>>("api/roles"))!
            .Single(item => item.Name == user!.Roles.Single());

        // Con usuarios asignados el rol no puede desactivarse: se retira primero.
        HttpResponseMessage blocked = await admin.PatchAsJsonAsync($"api/roles/{role.Id}/estado", new ChangeRoleStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        HttpResponseMessage unassign = await admin.PutAsJsonAsync($"api/usuarios/{userId}/roles", new AssignUserRolesRequest { Roles = [] });
        Assert.Equal(HttpStatusCode.OK, unassign.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/clientes")).StatusCode);
    }

    [Fact]
    public async Task DesactivarUsuario_InvalidaSuTokenVigente()
    {
        (string userName, int userId) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);
        HttpClient admin = await AdminAsync();

        HttpResponseMessage disable = await admin.PatchAsJsonAsync($"api/usuarios/{userId}/estado", new ChangeUserStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/clientes")).StatusCode);
    }

    [Fact]
    public async Task RestablecerContrasena_InvalidaElTokenAnterior()
    {
        (string userName, int userId) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);
        HttpClient admin = await AdminAsync();

        const string newPassword = "Nueva123*Clave";
        HttpResponseMessage reset = await admin.PutAsJsonAsync($"api/usuarios/{userId}/password", new ResetUserPasswordRequest { NewPassword = newPassword });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/clientes")).StatusCode);

        HttpClient renewed = await LoginAsync(userName, newPassword);
        Assert.Equal(HttpStatusCode.OK, (await renewed.GetAsync("api/clientes")).StatusCode);
    }
}
