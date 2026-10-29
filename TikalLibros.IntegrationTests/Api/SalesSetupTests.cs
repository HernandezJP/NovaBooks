using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Configuration;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Domain.Entities.Audit;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Base de Sprint 3: estados, parámetros, caja, permisos y auditoría.</summary>
public sealed class SalesSetupTests : ApiTestBase
{
    public SalesSetupTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private Task<List<PB_AUDITORIA>> AuditAsync(string entity, string? entityId) =>
        Factory.QueryAsync(context => context.Auditorias
            .Include(item => item.Detalles)
            .Where(item => item.AUD_Entidad == entity && item.AUD_EntidadId == entityId)
            .OrderBy(item => item.AUD_Auditoria)
            .ToListAsync());

    private static async Task<SupplierResponse> CreateSupplierAsync(HttpClient admin)
    {
        HttpResponseMessage response = await admin.PostAsJsonAsync("api/proveedores", new SaveSupplierRequest
        {
            PersonType = SaveSupplierRequest.LegalPerson,
            LegalName = $"Distribuidora {UniqueName("Peten")}",
            Nit = RandomDigits(8)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SupplierResponse>())!;
    }

    [Fact]
    public async Task Semilla_AgregaEstadosDeAprobacionParametrosYCajaSinDuplicar()
    {
        await Factory.QueryAsync(async context =>
        {
            await DatabaseSeeder.SeedAsync(context);
            return true;
        });

        var setup = await Factory.QueryAsync(async context => new
        {
            States = await context.EstadosPedido.Select(state => state.EPE_Codigo).ToListAsync(),
            Parameters = await context.ParametrosSistema.Select(parameter => parameter.PAR_Clave).ToListAsync(),
            CashRegisters = await context.Cajas.CountAsync(item => item.CAJ_Codigo == OrganizationSeeder.MainCashRegisterCode)
        });

        Assert.Contains(OrderStatus.PendingApproval, setup.States);
        Assert.Contains(OrderStatus.Rejected, setup.States);
        Assert.Contains(OrderStatus.Reserved, setup.States);
        Assert.Equal(setup.States.Count, setup.States.Distinct().Count());
        Assert.Equal(
            SystemParameterKeys.Defaults.Select(item => item.Key).Order(),
            setup.Parameters.Order());
        Assert.Equal(1, setup.CashRegisters);
    }

    [Fact]
    public async Task Roles_SupervisorApruebaPedidosYCajeroNo()
    {
        var permissions = await Factory.QueryAsync(context => context.RoleClaims
            .Where(claim => claim.ClaimType == CustomClaimTypes.Permission && claim.ClaimValue!.StartsWith("Pedidos."))
            .Join(context.Roles, claim => claim.RoleId, role => role.Id, (claim, role) => new { role.Name, claim.ClaimValue })
            .ToListAsync());

        Assert.Contains(permissions, item => item.Name == "Supervisor" && item.ClaimValue == OrderPermissions.Approve);
        Assert.Contains(permissions, item => item.Name == "Cajero" && item.ClaimValue == OrderPermissions.Create);
        Assert.DoesNotContain(permissions, item => item.Name == "Cajero" && item.ClaimValue == OrderPermissions.Approve);
    }

    [Fact]
    public async Task Parametros_SeLeenConSesionYSeModificanConPermiso()
    {
        HttpClient admin = await AdminAsync();

        List<SystemParameterResponse> parameters = (await admin.GetFromJsonAsync<List<SystemParameterResponse>>("api/parametros"))!;
        Assert.Equal("10", Assert.Single(parameters, item => item.Key == SystemParameterKeys.DiscountWithoutApproval).Value);

        HttpResponseMessage update = await admin.PutAsJsonAsync(
            $"api/parametros/{SystemParameterKeys.ReservationDays}",
            new UpdateSystemParameterRequest { Value = "5" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("5", (await update.Content.ReadFromJsonAsync<SystemParameterResponse>())!.Value);

        // Un solo registro (el del servicio, con detalle), no otro del filtro.
        PB_AUDITORIA audit = Assert.Single(await AuditAsync("ParametroSistema", SystemParameterKeys.ReservationDays));
        Assert.Equal("MODIFICAR", audit.AUD_Accion);
        Assert.NotNull(audit.AUD_UsuarioId);
        PB_AUDITORIA_DETALLE change = Assert.Single(audit.Detalles);
        Assert.Equal(("Valor", "3", "5"), (change.ADE_Campo, change.ADE_ValorAnterior, change.ADE_ValorNuevo));

        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync("api/parametros")).StatusCode);

        (string userName, _) = await CreateUserWithPermissionsAsync(OrderPermissions.View);
        HttpClient seller = await LoginAsync(userName, TestPassword);
        Assert.Equal(HttpStatusCode.OK, (await seller.GetAsync("api/parametros")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PutAsJsonAsync(
            $"api/parametros/{SystemParameterKeys.ReservationDays}",
            new UpdateSystemParameterRequest { Value = "7" })).StatusCode);
    }

    [Theory]
    [InlineData(SystemParameterKeys.DiscountWithoutApproval, "60", "no puede ser mayor que el descuento máximo")]
    [InlineData(SystemParameterKeys.MaximumDiscount, "5", "no puede ser menor que el descuento sin aprobación")]
    [InlineData(SystemParameterKeys.ReservationDays, "2.5", "número entero entre 1 y 30")]
    [InlineData(SystemParameterKeys.ReservationDays, "0", "número entero entre 1 y 30")]
    public async Task Parametros_ValoresIncoherentes_Devuelve400(string key, string value, string message)
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage response = await admin.PutAsJsonAsync($"api/parametros/{key}", new UpdateSystemParameterRequest { Value = value });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(message, await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Parametros_ConLetras_Devuelve400YParametroDesconocido404()
    {
        HttpClient admin = await AdminAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(
            $"api/parametros/{SystemParameterKeys.ReservationDays}", new UpdateSystemParameterRequest { Value = "3 dias" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync(
            "api/parametros/NO_EXISTE", new UpdateSystemParameterRequest { Value = "3" })).StatusCode);
    }

    [Fact]
    public async Task Auditoria_RegistraAltasYAccionesConUsuarioRutaEId()
    {
        HttpClient admin = await AdminAsync();
        SupplierResponse supplier = await CreateSupplierAsync(admin);

        PB_AUDITORIA created = Assert.Single(await AuditAsync("Proveedor", supplier.Id.ToString()));
        Assert.Equal("CREAR", created.AUD_Accion);
        Assert.Equal("POST", created.AUD_MetodoHttp);
        Assert.Equal("/api/proveedores", created.AUD_Ruta);
        Assert.NotNull(created.AUD_UsuarioId);
        Assert.False(string.IsNullOrEmpty(created.AUD_CorrelacionId));

        HttpResponseMessage order = await admin.PostAsJsonAsync("api/compras", new SavePurchaseOrderRequest
        {
            SupplierId = supplier.Id,
            Lines = [new PurchaseOrderLineRequest { BookId = await CreateBookAsync(admin), Quantity = 2, UnitCost = 10m }]
        });
        int orderId = (await order.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!.Id;
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"api/compras/{orderId}/emitir", null)).StatusCode);

        Assert.Equal(["CREAR", "EMITIR"], (await AuditAsync("OrdenCompra", orderId.ToString())).Select(item => item.AUD_Accion));
    }

    [Fact]
    public async Task Auditoria_NoRegistraConsultasNiOperacionesRechazadas()
    {
        HttpClient admin = await AdminAsync();
        int before = await Factory.QueryAsync(context => context.Auditorias.CountAsync());

        await admin.GetAsync("api/proveedores");
        HttpResponseMessage invalid = await admin.PostAsJsonAsync("api/proveedores", new SaveSupplierRequest());

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(before, await Factory.QueryAsync(context => context.Auditorias.CountAsync()));
    }

    [Fact]
    public async Task Login_QuedaEnElHistorialDeAccesos()
    {
        string user = TikalLibrosApiFactory.AdminUser;
        await AdminAsync();
        await PostLoginAsync(Anonymous(), user, "Incorrecta123*");

        List<PB_HISTORIAL_ACCESO> accesses = await Factory.QueryAsync(context => context.HistorialAccesos
            .Where(item => item.HAC_NombreUsuario == user)
            .OrderByDescending(item => item.HAC_HistorialAcceso)
            .Take(2)
            .ToListAsync());

        PB_HISTORIAL_ACCESO failed = accesses[0];
        PB_HISTORIAL_ACCESO succeeded = accesses[1];
        Assert.False(failed.HAC_Exitoso);
        Assert.Equal("Usuario o contraseña incorrectos.", failed.HAC_MotivoFallo);
        Assert.True(succeeded.HAC_Exitoso);
        Assert.NotNull(succeeded.HAC_UsuarioId);
        Assert.Null(succeeded.HAC_MotivoFallo);
    }

    private static async Task<int> CreateBookAsync(HttpClient admin)
    {
        ProductCatalogsResponse catalogs = (await admin.GetFromJsonAsync<ProductCatalogsResponse>("api/productos/catalogos"))!;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/productos", new SaveProductRequest
        {
            Title = Unique("Libro auditado "),
            Isbn13 = NewIsbn13(),
            LanguageId = catalogs.Languages.First().Id,
            FormatId = catalogs.Formats.First().Id,
            TaxId = catalogs.Taxes.First().Id,
            AllowsSale = true
        });

        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!.Id;
    }
}
