using System.Net;
using System.Net.Http.Json;
using TikalLibros.Application.DTOs.Employees;
using TikalLibros.Application.DTOs.Users;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

public sealed class EmployeesTests : ApiTestBase
{
    public EmployeesTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<SaveEmployeeRequest> NewEmployeeAsync(HttpClient client)
    {
        EmployeeCatalogsResponse catalogs = (await client.GetFromJsonAsync<EmployeeCatalogsResponse>("api/empleados/catalogos"))!;

        return new SaveEmployeeRequest
        {
            FirstName = "Lucía",
            LastName = UniqueName("Pérez"),
            Dpi = NewDpi(),
            Email = $"{Unique("empleado")}@tikal.test",
            PhoneTypeId = catalogs.PhoneTypes.First().Id,
            Phone = RandomDigits(8),
            PositionId = catalogs.Positions.First().Id,
            BranchId = catalogs.Branches.First().Id,
            HireDate = DateOnly.FromDateTime(DateTime.Today)
        };
    }

    private static async Task<EmployeeResponse> CreateAsync(HttpClient client, SaveEmployeeRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("api/empleados", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EmployeeResponse>())!;
    }

    private async Task<UserResponse> CreateUserForAsync(HttpClient admin, EmployeeResponse employee, string userName)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("api/usuarios", new CreateUserRequest
        {
            UserName = userName,
            Email = $"{userName}@tikal.test",
            Password = TestPassword,
            Roles = ["Compras"],
            EmployeeId = employee.Id
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    [Fact]
    public async Task Crear_AsignaCodigoYNormalizaDpi()
    {
        HttpClient admin = await AdminAsync();
        SaveEmployeeRequest request = await NewEmployeeAsync(admin);
        string dpi = request.Dpi;
        request.Dpi = $"{dpi[..4]} {dpi[4..9]} {dpi[9..]}";

        EmployeeResponse employee = await CreateAsync(admin, request);

        Assert.Matches(@"^EMP-\d{4}$", employee.Code);
        Assert.Equal(dpi, employee.Dpi);
        Assert.True(employee.IsActive);
        Assert.Null(employee.UserId);
    }

    [Fact]
    public async Task Crear_DatosInvalidos_DevuelveErroresPorCampo()
    {
        HttpClient admin = await AdminAsync();
        SaveEmployeeRequest request = await NewEmployeeAsync(admin);
        request.FirstName = "Lucía2";
        request.Dpi = "1234567800101";
        request.Phone = "12ab";

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/empleados", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("FirstName", out _));
        Assert.True(errors.TryGetProperty("Dpi", out _));
        Assert.True(errors.TryGetProperty("Phone", out _));
    }

    [Fact]
    public async Task Crear_DpiDuplicado_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        SaveEmployeeRequest first = await NewEmployeeAsync(admin);
        await CreateAsync(admin, first);

        SaveEmployeeRequest second = await NewEmployeeAsync(admin);
        second.Dpi = first.Dpi;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/empleados", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("DPI", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task CrearUsuarioParaEmpleado_LoVinculaYNoPermiteUnSegundo()
    {
        HttpClient admin = await AdminAsync();
        EmployeeResponse employee = await CreateAsync(admin, await NewEmployeeAsync(admin));

        UserResponse user = await CreateUserForAsync(admin, employee, Unique("lucia"));

        Assert.Equal(employee.Id, user.EmployeeId);
        Assert.Equal(employee.Code, user.EmployeeCode);
        Assert.Equal(user.UserName, (await admin.GetFromJsonAsync<EmployeeResponse>($"api/empleados/{employee.Id}"))!.UserName);

        HttpResponseMessage second = await admin.PostAsJsonAsync("api/usuarios", new CreateUserRequest
        {
            UserName = Unique("otro"),
            Email = $"{Unique("otro")}@tikal.test",
            Password = TestPassword,
            EmployeeId = employee.Id
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task DesactivarEmpleado_DesactivaSuUsuarioEInvalidaLaSesion()
    {
        HttpClient admin = await AdminAsync();
        EmployeeResponse employee = await CreateAsync(admin, await NewEmployeeAsync(admin));
        UserResponse user = await CreateUserForAsync(admin, employee, Unique("lucia"));
        HttpClient session = await LoginAsync(user.UserName, TestPassword);

        HttpResponseMessage disable = await admin.PatchAsJsonAsync($"api/empleados/{employee.Id}/estado", new ChangeEmployeeStatusRequest { IsActive = false });

        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        Assert.Equal(false, (await disable.Content.ReadFromJsonAsync<EmployeeResponse>())!.UserIsActive);
        Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("api/auth/me")).StatusCode);

        // El usuario no puede reactivarse mientras el empleado esté inactivo.
        HttpResponseMessage activate = await admin.PatchAsJsonAsync($"api/usuarios/{user.Id}/estado", new ChangeUserStatusRequest { IsActive = true });
        Assert.Equal(HttpStatusCode.Conflict, activate.StatusCode);
    }

    [Fact]
    public async Task VincularYDesvincularUsuarioExistente()
    {
        HttpClient admin = await AdminAsync();
        EmployeeResponse employee = await CreateAsync(admin, await NewEmployeeAsync(admin));
        (_, int userId) = await CreateUserWithPermissionsAsync("Clientes.Ver");

        HttpResponseMessage link = await admin.PutAsJsonAsync($"api/usuarios/{userId}/empleado", new AssignUserEmployeeRequest { EmployeeId = employee.Id });
        Assert.Equal(HttpStatusCode.OK, link.StatusCode);
        Assert.Equal(employee.Code, (await link.Content.ReadFromJsonAsync<UserResponse>())!.EmployeeCode);

        HttpResponseMessage unlink = await admin.PutAsJsonAsync($"api/usuarios/{userId}/empleado", new AssignUserEmployeeRequest());
        Assert.Equal(HttpStatusCode.OK, unlink.StatusCode);
        Assert.Null((await unlink.Content.ReadFromJsonAsync<UserResponse>())!.EmployeeId);
    }

    [Fact]
    public async Task Administrador_TieneEmpleadoInicial()
    {
        HttpClient admin = await AdminAsync();
        UserResponse administrator = (await admin.GetFromJsonAsync<Application.Common.PagedResponse<UserResponse>>("api/usuarios?search=admin"))!
            .Items.Single(user => user.UserName == TikalLibrosApiFactory.AdminUser);

        Assert.NotNull(administrator.EmployeeCode);
    }

    [Fact]
    public async Task SinPermisoDeEmpleados_Devuelve403()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Clientes.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/empleados")).StatusCode);
    }
}
