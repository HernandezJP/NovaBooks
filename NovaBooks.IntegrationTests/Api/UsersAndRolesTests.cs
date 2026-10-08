using System.Net;
using System.Net.Http.Json;
using NovaBooks.Application.Authentication;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.DTOs.Users;
using NovaBooks.IntegrationTests.Infrastructure;

namespace NovaBooks.IntegrationTests.Api;

/// <summary>Reglas de negocio de usuarios y roles.</summary>
public sealed class UsersAndRolesTests : ApiTestBase
{
    public UsersAndRolesTests(NovaBooksApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CrearUsuario_NombreDuplicado_Devuelve409()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/usuarios", new CreateUserRequest
        {
            UserName = userName,
            Email = $"{Unique("otro")}@novabooks.test",
            Password = TestPassword,
            Roles = []
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("El nombre de usuario ya está registrado.", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task CrearUsuario_DatosInvalidos_Devuelve400ConErroresPorCampo()
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/usuarios", new CreateUserRequest
        {
            UserName = "x",
            Email = "no-es-correo",
            Password = "123",
            Roles = []
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("UserName", out _));
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Administrador_NoPuedeDesactivarseASiMismo()
    {
        HttpClient admin = await AdminAsync();
        AuthenticatedUserResponse? me = await admin.GetFromJsonAsync<AuthenticatedUserResponse>("api/auth/me");

        HttpResponseMessage response = await admin.PatchAsJsonAsync($"api/usuarios/{me!.Id}/estado", new ChangeUserStatusRequest { IsActive = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RolAdministrador_EsProtegido()
    {
        HttpClient admin = await AdminAsync();
        RoleResponse administrator = (await admin.GetFromJsonAsync<List<RoleResponse>>("api/roles"))!
            .Single(role => role.Name == "Administrador");

        Assert.True(administrator.IsProtected);

        HttpResponseMessage permissions = await admin.PutAsJsonAsync(
            $"api/roles/{administrator.Id}/permisos",
            new AssignRolePermissionsRequest { Permissions = ["Clientes.Ver"] });
        Assert.Equal(HttpStatusCode.Conflict, permissions.StatusCode);

        HttpResponseMessage disable = await admin.PatchAsJsonAsync(
            $"api/roles/{administrator.Id}/estado",
            new ChangeRoleStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.Conflict, disable.StatusCode);
    }

    [Fact]
    public async Task CrearRol_NombreDuplicado_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        string name = Unique("Rol");

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/roles", new CreateRoleRequest { Name = name })).StatusCode);

        HttpResponseMessage duplicate = await admin.PostAsJsonAsync("api/roles", new CreateRoleRequest { Name = name });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task AsignarPermisos_PermisoInexistente_SeRechaza()
    {
        HttpClient admin = await AdminAsync();
        HttpResponseMessage created = await admin.PostAsJsonAsync("api/roles", new CreateRoleRequest { Name = Unique("Rol") });
        RoleResponse role = (await created.Content.ReadFromJsonAsync<RoleResponse>())!;

        HttpResponseMessage response = await admin.PutAsJsonAsync(
            $"api/roles/{role.Id}/permisos",
            new AssignRolePermissionsRequest { Permissions = ["Permiso.Inventado"] });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EliminarRol_SinUsuarios_EsBorradoLogico()
    {
        HttpClient admin = await AdminAsync();
        HttpResponseMessage created = await admin.PostAsJsonAsync("api/roles", new CreateRoleRequest { Name = Unique("Rol") });
        RoleResponse role = (await created.Content.ReadFromJsonAsync<RoleResponse>())!;

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"api/roles/{role.Id}")).StatusCode);

        List<RoleResponse>? roles = await admin.GetFromJsonAsync<List<RoleResponse>>("api/roles");
        Assert.False(roles!.Single(item => item.Id == role.Id).IsActive);
    }
}
