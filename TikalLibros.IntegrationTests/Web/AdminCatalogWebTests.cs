extern alias web;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using TikalLibros.Application.DTOs.Categories;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Organization;
using web::TikalLibros.Web.Components.Pages.Categories;
using web::TikalLibros.Web.Components.Shared;
using web::TikalLibros.Web.Components.Pages.Organization;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Pantallas de los catálogos base.</summary>
public sealed class AdminCatalogWebTests : WebTestContext
{
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task Categorias_SubcategoriaSangradaYBotonSoloConPermiso()
    {
        Handler.Respond = _ => Json(new List<CategoryResponse>
        {
            new() { Id = 1, Code = "LITERATURA", Name = "Literatura", SubcategoriesCount = 1, IsActive = true },
            new() { Id = 9, Code = "NOVELA_NEGRA", Name = "Novela negra", ParentId = 1, Parent = "Literatura", BooksCount = 4, IsActive = true }
        });

        await Session.SignInAsync(ValidLogin("Catalogo.Ver"));
        IRenderedComponent<Categories> page = Render<Categories>();

        page.WaitForAssertion(() => Assert.Single(page.FindAll(".category-child")));
        Assert.Contains("Novela negra", page.Find(".category-child").TextContent);
        Assert.DoesNotContain("Nueva categoría", page.Markup);
        Assert.Equal("/api/categorias?isActive=true", Handler.LastPathAndQuery);

        await Session.SignInAsync(ValidLogin("Catalogo.Ver", "Catalogo.Crear"));
        IRenderedComponent<Categories> withCreate = Render<Categories>();
        withCreate.WaitForAssertion(() => Assert.Contains("Nueva categoría", withCreate.Markup));
    }

    [Fact]
    public async Task Almacenes_MuestraPrincipalYExistencias()
    {
        Handler.Respond = _ => Json(new List<WarehouseResponse>
        {
            new() { Id = 1, Code = "ALM-01", Name = "Bodega principal", Branch = "Sucursal central", IsMain = true, IsActive = true, BooksWithStock = 12, Units = 340 }
        });

        await Session.SignInAsync(ValidLogin("Configuracion.Organizacion"));
        IRenderedComponent<Warehouses> page = Render<Warehouses>();

        page.WaitForAssertion(() => Assert.Contains("Bodega principal", page.Markup));
        Assert.Contains("Principal", page.Markup);
        Assert.Contains("340", page.Markup);
    }

    [Fact]
    public async Task Sucursal_EditarEnviaDireccionYConservaPrincipal()
    {
        await Session.SignInAsync(ValidLogin("Configuracion.Organizacion"));
        string? sentBody = null;

        Handler.Respond = request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json(new OrganizationCatalogsResponse
                {
                    Departments =
                    [
                        new DepartmentOptionResponse
                        {
                            Id = 9, Name = "Quetzaltenango",
                            Municipalities = [new CatalogOptionResponse { Id = 120, Code = "0901", Name = "Quetzaltenango" }]
                        }
                    ]
                });
            }

            sentBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(new BranchResponse { Id = 3, Code = "SUC-02", Name = "Sucursal Xela", IsMain = true, IsActive = true });
        };

        BranchResponse branch = new()
        {
            Id = 3, Code = "SUC-02", Name = "Sucursal Xela", DepartmentId = 9, Department = "Quetzaltenango",
            MunicipalityId = 120, Municipality = "Quetzaltenango", Zone = "3", AddressLine = "4a. calle 10-25", IsMain = true, IsActive = true
        };

        IRenderedComponent<MudDialogProvider> provider = Render<MudDialogProvider>();
        IDialogService dialogs = Services.GetRequiredService<IDialogService>();
        Task<BranchResponse?>? shown = null;

        await provider.InvokeAsync(() =>
        {
            shown = dialogs.ShowFormAsync<BranchFormDialog, BranchResponse>(
                "Editar sucursal", new DialogParameters<BranchFormDialog> { { dialog => dialog.Branch, branch } });
        });

        provider.WaitForAssertion(() => Assert.NotEmpty(provider.FindAll("form")));
        Assert.Contains("Es la sucursal principal", provider.Markup);
        provider.Find("form").Submit();

        Assert.NotNull(await shown!.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("/api/sucursales/3", Handler.LastPath);

        using JsonDocument body = JsonDocument.Parse(sentBody!);
        Assert.Equal(120, body.RootElement.GetProperty("municipalityId").GetInt32());
        Assert.Equal("4a. calle 10-25", body.RootElement.GetProperty("addressLine").GetString());
        Assert.True(body.RootElement.GetProperty("isMain").GetBoolean());
    }
}
