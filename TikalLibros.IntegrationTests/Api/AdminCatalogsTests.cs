using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.DTOs.Categories;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.DTOs.PriceLists;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Catálogos base: categorías, listas de precio, sucursales, almacenes y
/// cajas, con sus reglas de unicidad, principal/predeterminada y bloqueos al
/// desactivar.
/// </summary>
public sealed class AdminCatalogsTests : SalesTestBase
{
    public AdminCatalogsTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static Task<HttpResponseMessage> StatusAsync(HttpClient client, string path, int id, bool isActive) =>
        client.PatchAsJsonAsync($"{path}/{id}/estado", new ChangeActiveStatusRequest { IsActive = isActive });

    private static async Task<int> MunicipalityAsync(HttpClient admin, string department, string municipality)
    {
        OrganizationCatalogsResponse catalogs = (await admin.GetFromJsonAsync<OrganizationCatalogsResponse>("api/sucursales/catalogos"))!;
        return catalogs.Departments.Single(item => item.Name == department).Municipalities.Single(item => item.Name == municipality).Id;
    }

    private static async Task<BranchResponse> CreateBranchAsync(HttpClient admin, bool isMain = false) =>
        await OkAsync<BranchResponse>(await admin.PostAsJsonAsync("api/sucursales", new SaveBranchRequest
        {
            Name = Unique("Sucursal "),
            MunicipalityId = await MunicipalityAsync(admin, "Quetzaltenango", "Quetzaltenango"),
            Zone = "3",
            AddressLine = "4a. calle 10-25",
            Phone = "7765 4321",
            IsMain = isMain
        }), HttpStatusCode.Created);

    [Fact]
    public async Task Geografia_TieneLos22DepartamentosConSusMunicipios()
    {
        HttpClient admin = await AdminAsync();

        OrganizationCatalogsResponse catalogs = (await admin.GetFromJsonAsync<OrganizationCatalogsResponse>("api/sucursales/catalogos"))!;

        Assert.Equal(22, catalogs.Departments.Count);
        // Mismos municipios por departamento que valida el DPI (338 en total).
        Assert.Equal(338, catalogs.Departments.Sum(item => item.Municipalities.Count));
        Assert.Equal(32, catalogs.Departments.Single(item => item.Name == "Huehuetenango").Municipalities.Count);
        Assert.Contains(catalogs.Departments.Single(item => item.Name == "Petén").Municipalities, item => item.Name == "Flores");
    }

    [Fact]
    public async Task Categoria_CodigoDesdeNombreSubcategoriaYDuplicadoEnElMismoPadre()
    {
        HttpClient admin = await AdminAsync();
        string name = Unique("Ciencia ficción ");

        CategoryResponse parent = await OkAsync<CategoryResponse>(
            await admin.PostAsJsonAsync("api/categorias", new SaveCategoryRequest { Name = name }), HttpStatusCode.Created);

        Assert.StartsWith("CIENCIA_FICCION_", parent.Code);
        Assert.Null(parent.ParentId);

        CategoryResponse child = await OkAsync<CategoryResponse>(
            await admin.PostAsJsonAsync("api/categorias", new SaveCategoryRequest { Name = "Distopías", ParentId = parent.Id }), HttpStatusCode.Created);

        Assert.Equal((parent.Id, name), (child.ParentId!.Value, child.Parent));

        // El mismo nombre se permite bajo otro padre, pero no dos veces bajo el mismo.
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("api/categorias", new SaveCategoryRequest { Name = "Distopías", ParentId = parent.Id })).StatusCode);
    }

    [Fact]
    public async Task Categoria_NoAdmiteCiclosNiDesactivarConSubcategoriasActivas()
    {
        HttpClient admin = await AdminAsync();

        CategoryResponse parent = await OkAsync<CategoryResponse>(
            await admin.PostAsJsonAsync("api/categorias", new SaveCategoryRequest { Name = Unique("Historia ") }), HttpStatusCode.Created);
        CategoryResponse child = await OkAsync<CategoryResponse>(
            await admin.PostAsJsonAsync("api/categorias", new SaveCategoryRequest { Name = "Historia maya", ParentId = parent.Id }), HttpStatusCode.Created);

        HttpResponseMessage cycle = await admin.PutAsJsonAsync($"api/categorias/{parent.Id}", new SaveCategoryRequest { Name = parent.Name, ParentId = child.Id });
        Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
        Assert.Contains("subcategorías", await ReadProblemDetailAsync(cycle));

        Assert.Equal(HttpStatusCode.Conflict, (await StatusAsync(admin, "api/categorias", parent.Id, false)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await StatusAsync(admin, "api/categorias", child.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await StatusAsync(admin, "api/categorias", parent.Id, false)).StatusCode);

        // Una subcategoría no se reactiva mientras su padre esté inactivo.
        Assert.Equal(HttpStatusCode.Conflict, (await StatusAsync(admin, "api/categorias", child.Id, true)).StatusCode);
    }

    [Fact]
    public async Task Categoria_NombreConCaracteresNoPermitidos_Devuelve400()
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/categorias", new SaveCategoryRequest { Name = "<script>" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListaPrecio_SoloUnaPredeterminadaQueNoSeDesactiva()
    {
        HttpClient admin = await AdminAsync();
        PriceListResponse original = (await admin.GetFromJsonAsync<List<PriceListResponse>>("api/listas-precio"))!.Single(item => item.IsDefault);

        PriceListResponse list = await OkAsync<PriceListResponse>(
            await admin.PostAsJsonAsync("api/listas-precio", new SavePriceListRequest { Name = Unique("Precio feria "), IsDefault = true }),
            HttpStatusCode.Created);

        List<PriceListResponse> lists = (await admin.GetFromJsonAsync<List<PriceListResponse>>("api/listas-precio?isActive=true"))!;
        Assert.Equal(list.Id, lists.Single(item => item.IsDefault).Id);

        Assert.Equal(HttpStatusCode.Conflict, (await StatusAsync(admin, "api/listas-precio", list.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"api/listas-precio/{list.Id}", new SavePriceListRequest { Name = list.Name, IsDefault = false })).StatusCode);

        // Se devuelve la predeterminada original y la de prueba ya puede desactivarse.
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync($"api/listas-precio/{original.Id}", new SavePriceListRequest { Name = original.Name, Description = original.Description, IsDefault = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await StatusAsync(admin, "api/listas-precio", list.Id, false)).StatusCode);
    }

    [Fact]
    public async Task Sucursal_CodigoCorrelativoDireccionYAuditoria()
    {
        HttpClient admin = await AdminAsync();

        BranchResponse branch = await CreateBranchAsync(admin);

        Assert.Matches(@"^SUC-\d{2,}$", branch.Code);
        Assert.Equal(("Quetzaltenango", "Quetzaltenango", "3", false), (branch.Department, branch.Municipality, branch.Zone, branch.IsMain));
        Assert.Equal("77654321", branch.Phone);

        Assert.True(await Factory.QueryAsync(context => context.Auditorias.AnyAsync(item =>
            item.AUD_Entidad == "Sucursal" && item.AUD_EntidadId == branch.Id.ToString())));

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/sucursales", new SaveBranchRequest
        {
            Name = branch.Name,
            MunicipalityId = branch.MunicipalityId,
            AddressLine = "Otra dirección 1-1"
        })).StatusCode);
    }

    [Fact]
    public async Task Sucursal_PrincipalSeTransfiereYNoSeDesactiva()
    {
        HttpClient admin = await AdminAsync();
        BranchResponse original = (await admin.GetFromJsonAsync<List<BranchResponse>>("api/sucursales"))!.Single(item => item.IsMain);

        BranchResponse branch = await CreateBranchAsync(admin, isMain: true);

        List<BranchResponse> branches = (await admin.GetFromJsonAsync<List<BranchResponse>>("api/sucursales?isActive=true"))!;
        Assert.Equal(branch.Id, branches.Single(item => item.IsMain).Id);
        Assert.Equal(HttpStatusCode.Conflict, (await StatusAsync(admin, "api/sucursales", branch.Id, false)).StatusCode);

        // Se restituye la principal original para no afectar a las demás pruebas.
        SaveBranchRequest restore = new()
        {
            Name = original.Name,
            MunicipalityId = original.MunicipalityId,
            Zone = original.Zone,
            AddressLine = original.AddressLine,
            AddressReference = original.AddressReference,
            Phone = original.Phone,
            Email = original.Email,
            IsMain = true
        };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"api/sucursales/{original.Id}", restore)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await StatusAsync(admin, "api/sucursales", branch.Id, false)).StatusCode);
    }

    [Fact]
    public async Task Almacen_PrimeroEsPrincipalYBloqueaDesactivarSucursal()
    {
        HttpClient admin = await AdminAsync();
        BranchResponse branch = await CreateBranchAsync(admin);

        WarehouseResponse first = await OkAsync<WarehouseResponse>(
            await admin.PostAsJsonAsync("api/almacenes", new SaveWarehouseRequest { BranchId = branch.Id, Name = "Bodega Xela" }), HttpStatusCode.Created);
        WarehouseResponse second = await OkAsync<WarehouseResponse>(
            await admin.PostAsJsonAsync("api/almacenes", new SaveWarehouseRequest { BranchId = branch.Id, Name = "Bodega anexa", IsMain = true }), HttpStatusCode.Created);

        Assert.Matches(@"^ALM-\d{2,}$", first.Code);
        List<WarehouseResponse> warehouses = (await admin.GetFromJsonAsync<List<WarehouseResponse>>("api/almacenes?search=" + Uri.EscapeDataString(branch.Name)))!;
        Assert.Equal(second.Id, warehouses.Single(item => item.IsMain).Id);

        HttpResponseMessage blocked = await StatusAsync(admin, "api/sucursales", branch.Id, false);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Contains("almacenes", await ReadProblemDetailAsync(blocked));

        Assert.Equal(HttpStatusCode.Conflict, (await StatusAsync(admin, "api/almacenes", second.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await StatusAsync(admin, "api/almacenes", first.Id, false)).StatusCode);
    }

    [Fact]
    public async Task Almacen_ConExistencias_NoSeDesactivaNiCambiaDeSucursal()
    {
        HttpClient admin = await AdminAsync();
        BranchResponse main = (await admin.GetFromJsonAsync<List<BranchResponse>>("api/sucursales"))!.Single(item => item.IsMain);
        BranchResponse other = await CreateBranchAsync(admin);
        int book = await CreateBookAsync(admin);

        WarehouseResponse warehouse = await OkAsync<WarehouseResponse>(
            await admin.PostAsJsonAsync("api/almacenes", new SaveWarehouseRequest { BranchId = main.Id, Name = Unique("Bodega temporal ") }), HttpStatusCode.Created);

        await Factory.QueryAsync(async context =>
        {
            context.Existencias.Add(new PB_EXISTENCIA { EXI_AlmacenId = warehouse.Id, EXI_LibroId = book, EXI_CantidadDisponible = 5 });
            return await context.SaveChangesAsync();
        });

        HttpResponseMessage deactivate = await StatusAsync(admin, "api/almacenes", warehouse.Id, false);
        Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
        Assert.Contains("existencias", await ReadProblemDetailAsync(deactivate));

        HttpResponseMessage move = await admin.PutAsJsonAsync($"api/almacenes/{warehouse.Id}",
            new SaveWarehouseRequest { BranchId = other.Id, Name = warehouse.Name });
        Assert.Equal(HttpStatusCode.BadRequest, move.StatusCode);

        WarehouseResponse listed = (await admin.GetFromJsonAsync<WarehouseResponse>($"api/almacenes/{warehouse.Id}"))!;
        Assert.Equal((1, 5m), (listed.BooksWithStock, listed.Units));
    }

    [Fact]
    public async Task Caja_AbiertaNoSeDesactivaYSucursalInactivaNoAdmiteCajas()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync(CashPermissions.Open, CashPermissions.View);
        int registerId = (await EnsureCashAsync(cashier)).CashRegisterId;

        CashRegisterAdminResponse register = (await admin.GetFromJsonAsync<CashRegisterAdminResponse>($"api/cajas/{registerId}"))!;
        Assert.True(register.IsOpen);
        Assert.NotNull(register.OpenedBy);

        HttpResponseMessage blocked = await StatusAsync(admin, "api/cajas", register.Id, false);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Contains("abierta", await ReadProblemDetailAsync(blocked));

        BranchResponse branch = await CreateBranchAsync(admin);
        CashRegisterAdminResponse created = await OkAsync<CashRegisterAdminResponse>(
            await admin.PostAsJsonAsync("api/cajas", new SaveCashRegisterRequest { BranchId = branch.Id, Name = "Caja 1" }), HttpStatusCode.Created);
        Assert.Matches(@"^CAJA-\d{2,}$", created.Code);

        Assert.Equal(HttpStatusCode.OK, (await StatusAsync(admin, "api/cajas", created.Id, false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await StatusAsync(admin, "api/sucursales", branch.Id, false)).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("api/cajas", new SaveCashRegisterRequest { BranchId = branch.Id, Name = "Caja 2" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await StatusAsync(admin, "api/cajas", created.Id, true)).StatusCode);
    }

    [Fact]
    public async Task Permisos_OrganizacionSoloConSuPermisoYCatalogosConLosDeCatalogo()
    {
        (HttpClient viewer, _) = await EmployeeClientAsync(CatalogPermissions.View);

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("api/sucursales")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("api/almacenes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("api/cajas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("api/categorias")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("api/listas-precio")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync("api/categorias", new SaveCategoryRequest { Name = Unique("Sin permiso ") })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync("api/listas-precio", new SavePriceListRequest { Name = Unique("Sin permiso ") })).StatusCode);

        (HttpClient organizer, _) = await EmployeeClientAsync(ConfigurationPermissions.ManageOrganization);
        Assert.Equal(HttpStatusCode.OK, (await organizer.GetAsync("api/sucursales")).StatusCode);
    }
}
