extern alias web;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Customers;
using web::TikalLibros.Web.Components.Shared;
using web::TikalLibros.Web.Components.Shared.Sales;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Alta rápida de cliente desde la venta o el pedido.</summary>
public sealed class QuickCustomerTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value) };

    private static readonly CustomerCatalogsResponse Catalogs = new()
    {
        PersonTypes =
        [
            new CatalogOptionResponse { Id = 1, Code = SaveCustomerRequest.NaturalPerson, Name = "Persona individual" },
            new CatalogOptionResponse { Id = 2, Code = SaveCustomerRequest.LegalPerson, Name = "Persona jurídica" }
        ],
        PhoneTypes =
        [
            new CatalogOptionResponse { Id = 5, Code = "FIJO", Name = "Fijo" },
            new CatalogOptionResponse { Id = 6, Code = "CELULAR", Name = "Celular" }
        ]
    };

    [Fact]
    public async Task Buscador_MuestraNuevoClienteSoloConPermisoDeCrear()
    {
        await Session.SignInAsync(ValidLogin("Clientes.Ver"));
        IRenderedComponent<CustomerPicker> withoutCreate = Render<CustomerPicker>();
        Assert.DoesNotContain("Nuevo cliente", withoutCreate.Markup);

        await Session.SignInAsync(ValidLogin("Clientes.Ver", "Clientes.Crear"));
        IRenderedComponent<CustomerPicker> withCreate = Render<CustomerPicker>();
        Assert.Contains("Nuevo cliente", withCreate.Markup);

        IRenderedComponent<CustomerPicker> disabled = Render<CustomerPicker>(parameters => parameters.Add(p => p.AllowCreate, false));
        Assert.DoesNotContain("Nuevo cliente", disabled.Markup);
    }

    [Fact]
    public async Task Dialogo_RegistraElClienteYLoDevuelveParaUsarlo()
    {
        await Session.SignInAsync(ValidLogin("Clientes.Ver", "Clientes.Crear"));
        string? sentBody = null;

        Handler.Respond = request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json(Catalogs);
            }

            sentBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(new CustomerResponse
            {
                Id = 41,
                Code = "CLI-000041",
                PersonType = SaveCustomerRequest.NaturalPerson,
                DisplayName = "Ana Morales",
                Nit = "1234567K"
            }, HttpStatusCode.Created);
        };

        IRenderedComponent<MudDialogProvider> provider = Render<MudDialogProvider>();
        IDialogService dialogs = Services.GetRequiredService<IDialogService>();
        Task<CustomerListItemResponse?>? shown = null;

        await provider.InvokeAsync(() =>
        {
            shown = dialogs.ShowFormAsync<QuickCustomerDialog, CustomerListItemResponse>(
                "Nuevo cliente",
                new DialogParameters<QuickCustomerDialog> { { dialog => dialog.InitialName, "Ana Morales" } });
        });

        // El texto buscado llena nombre y apellido.
        provider.WaitForAssertion(() => Assert.Equal("Ana", provider.FindAll(".tl-filter-name input")[0].GetAttribute("value")));
        Assert.Equal("Morales", provider.FindAll(".tl-filter-name input")[2].GetAttribute("value"));

        provider.Find(".tl-filter-nit input").Change("1234567-k");
        provider.Find(".tl-filter-phone input").Change("55123456");
        provider.Find("form").Submit();

        CustomerListItemResponse? customer = await shown!.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("/api/clientes", Handler.LastPath);
        Assert.Equal((41, "CLI-000041", "Ana Morales"), (customer!.Id, customer.Code, customer.DisplayName));

        using JsonDocument body = JsonDocument.Parse(sentBody!);
        Assert.Equal("Ana", body.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("55123456", body.RootElement.GetProperty("phone").GetString());
        Assert.Equal(6, body.RootElement.GetProperty("phoneTypeId").GetInt32()); // celular
    }

    [Fact]
    public async Task Dialogo_ConNombreVacio_NoEnviaNada()
    {
        await Session.SignInAsync(ValidLogin("Clientes.Ver", "Clientes.Crear"));
        int posts = 0;
        Handler.Respond = request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                posts++;
            }

            return Json(Catalogs);
        };

        IRenderedComponent<MudDialogProvider> provider = Render<MudDialogProvider>();
        IDialogService dialogs = Services.GetRequiredService<IDialogService>();

        // No se espera el resultado: el diálogo queda abierto durante la prueba.
        await provider.InvokeAsync(() =>
        {
            _ = dialogs.ShowFormAsync<QuickCustomerDialog, CustomerListItemResponse>("Nuevo cliente", new DialogParameters<QuickCustomerDialog>());
        });

        provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll("form")));
        provider.Find("form").Submit();

        provider.WaitForAssertion(() => Assert.Contains("El primer nombre es obligatorio.", provider.Markup));
        Assert.Equal(0, posts);
    }
}
