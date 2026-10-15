using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Customers;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Domain.Entities.Purchasing;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

public sealed class SuppliersTests : ApiTestBase
{
    public SuppliersTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static SaveSupplierRequest NewSupplier() => new()
    {
        PersonType = SaveSupplierRequest.LegalPerson,
        LegalName = $"Distribuidora {UniqueName("Maya")}, S.A.",
        TradeName = "Libros Maya",
        Nit = RandomDigits(7) + "K",
        Email = $"{Unique("ventas")}@proveedor.test",
        CreditLimit = 5000m,
        CreditDays = 30
    };

    private static async Task<SupplierResponse> CreateAsync(HttpClient client, SaveSupplierRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("api/proveedores", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SupplierResponse>())!;
    }

    [Fact]
    public async Task Crear_AsignaCodigoYNormalizaNit()
    {
        HttpClient admin = await AdminAsync();
        SaveSupplierRequest request = NewSupplier();
        string nit = request.Nit;
        request.Nit = nit[..4] + "-" + nit[4..].ToLowerInvariant();

        SupplierResponse supplier = await CreateAsync(admin, request);

        Assert.Matches(@"^PRO-\d{4}$", supplier.Code);
        Assert.Equal(nit, supplier.Nit);
        Assert.Equal(request.LegalName, supplier.DisplayName);
        Assert.Equal(0, supplier.OpenOrders);
    }

    [Fact]
    public async Task Crear_NitDeUnCliente_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        string nit = RandomDigits(7) + "K";
        CustomerCatalogsResponse catalogs = (await admin.GetFromJsonAsync<CustomerCatalogsResponse>("api/clientes/catalogos"))!;
        HttpResponseMessage customer = await admin.PostAsJsonAsync("api/clientes", new SaveCustomerRequest
        {
            PersonType = SaveCustomerRequest.NaturalPerson,
            FirstName = "Ana",
            LastName = "López",
            Nit = nit
        });
        Assert.Equal(HttpStatusCode.Created, customer.StatusCode);

        SaveSupplierRequest request = NewSupplier();
        request.Nit = nit;

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/proveedores", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("cliente", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Crear_DatosInvalidos_DevuelveTodosLosErrores()
    {
        HttpClient admin = await AdminAsync();
        SaveSupplierRequest request = NewSupplier();
        request.LegalName = "Libros @ Guate";
        request.Nit = "ABC";
        request.Phone = "55a";

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/proveedores", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("LegalName", out _));
        Assert.True(errors.TryGetProperty("Nit", out _));
        Assert.True(errors.TryGetProperty("Phone", out _));
    }

    [Fact]
    public async Task Actualizar_CambioDeTipoDePersona_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        SaveSupplierRequest request = NewSupplier();
        SupplierResponse supplier = await CreateAsync(admin, request);

        request.PersonType = SaveSupplierRequest.NaturalPerson;
        request.FirstName = "Carlos";
        request.LastName = "Ruiz";

        HttpResponseMessage response = await admin.PutAsJsonAsync($"api/proveedores/{supplier.Id}", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Desactivar_ConOrdenAbierta_Devuelve409YSinOrdenesSePermite()
    {
        HttpClient admin = await AdminAsync();
        SupplierResponse supplier = await CreateAsync(admin, NewSupplier());

        int orderId = await Factory.QueryAsync(async context =>
        {
            PB_ORDEN_COMPRA order = new()
            {
                OCO_Numero = Unique("OC-"),
                OCO_ProveedorId = supplier.Id,
                OCO_SucursalId = await context.Sucursales.Select(item => item.SUC_Sucursal).FirstAsync(),
                OCO_MonedaId = await context.Monedas.Select(item => item.MON_Moneda).FirstAsync(),
                OCO_EstadoOrdenCompraId = await context.EstadosOrdenCompra
                    .Where(item => item.EOC_Codigo == "EMITIDA").Select(item => item.EOC_EstadoOrdenCompra).FirstAsync(),
                OCO_FechaEmision = DateTime.UtcNow,
                OCO_TipoCambio = 1,
                OCO_Activo = true
            };
            context.OrdenesCompra.Add(order);
            await context.SaveChangesAsync();
            return order.OCO_OrdenCompra;
        });

        HttpResponseMessage blocked = await admin.PatchAsJsonAsync($"api/proveedores/{supplier.Id}/estado", new ChangeSupplierStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Contains("orden", await ReadProblemDetailAsync(blocked));

        await Factory.QueryAsync(async context =>
        {
            PB_ORDEN_COMPRA order = await context.OrdenesCompra.SingleAsync(item => item.OCO_OrdenCompra == orderId);
            order.OCO_EstadoOrdenCompraId = await context.EstadosOrdenCompra
                .Where(item => item.EOC_Codigo == "CANCELADA").Select(item => item.EOC_EstadoOrdenCompra).FirstAsync();
            return await context.SaveChangesAsync();
        });

        HttpResponseMessage allowed = await admin.PatchAsJsonAsync($"api/proveedores/{supplier.Id}/estado", new ChangeSupplierStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        PagedResponse<SupplierListItemResponse>? active =
            await admin.GetFromJsonAsync<PagedResponse<SupplierListItemResponse>>($"api/proveedores?search={supplier.Code}&isActive=true");
        Assert.Empty(active!.Items);
    }

    [Fact]
    public async Task SoloLectura_NoPuedeCrear()
    {
        (string userName, _) = await CreateUserWithPermissionsAsync("Proveedores.Ver");
        HttpClient client = await LoginAsync(userName, TestPassword);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/proveedores")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/proveedores", NewSupplier())).StatusCode);
    }
}
