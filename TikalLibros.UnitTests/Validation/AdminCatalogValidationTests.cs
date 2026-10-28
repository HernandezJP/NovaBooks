using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Categories;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.DTOs.PriceLists;
using TikalLibros.Infrastructure.Services.Organization;

namespace TikalLibros.UnitTests.Validation;

/// <summary>Catálogos base: validación de entradas y códigos que asigna el sistema.</summary>
public sealed class AdminCatalogValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string member) =>
        results.Any(result => result.MemberNames.Contains(member));

    private static SaveBranchRequest ValidBranch() => new()
    {
        Name = "Sucursal Xela",
        MunicipalityId = 5,
        Zone = "3",
        AddressLine = "4a. calle 10-25",
        Phone = "7765-4321",
        Email = "xela@tikallibros.gt"
    };

    [Fact]
    public void Sucursal_Valida_NoTieneErrores()
    {
        Assert.Empty(Validate(ValidBranch()));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("12a")]
    [InlineData("-1")]
    public void Sucursal_ZonaQueNoEsNumeroDeHastaDosDigitos_EsInvalida(string zone)
    {
        SaveBranchRequest branch = ValidBranch();
        branch.Zone = zone;

        Assert.True(HasErrorFor(Validate(branch), nameof(SaveBranchRequest.Zone)));
    }

    [Fact]
    public void Sucursal_SinMunicipioNiDireccion_EsInvalida()
    {
        SaveBranchRequest branch = ValidBranch();
        branch.MunicipalityId = null;
        branch.AddressLine = "1a";

        List<ValidationResult> results = Validate(branch);

        Assert.True(HasErrorFor(results, nameof(SaveBranchRequest.MunicipalityId)));
        Assert.True(HasErrorFor(results, nameof(SaveBranchRequest.AddressLine)));
    }

    [Fact]
    public void Sucursal_DireccionConSignosDeEtiqueta_EsInvalida()
    {
        SaveBranchRequest branch = ValidBranch();
        branch.AddressReference = "Frente al <b>parque</b>";

        Assert.True(HasErrorFor(Validate(branch), nameof(SaveBranchRequest.AddressReference)));
    }

    [Fact]
    public void AlmacenYCaja_SinSucursal_SonInvalidos()
    {
        Assert.True(HasErrorFor(Validate(new SaveWarehouseRequest { Name = "Bodega" }), nameof(SaveWarehouseRequest.BranchId)));
        Assert.True(HasErrorFor(Validate(new SaveCashRegisterRequest { Name = "Caja" }), nameof(SaveCashRegisterRequest.BranchId)));
    }

    [Theory]
    [InlineData("Ciencia @ficción")]
    [InlineData("-Infantil")]
    [InlineData("")]
    public void CategoriaYLista_NombreInvalido(string name)
    {
        Assert.True(HasErrorFor(Validate(new SaveCategoryRequest { Name = name }), nameof(SaveCategoryRequest.Name)));
        Assert.True(HasErrorFor(Validate(new SavePriceListRequest { Name = name }), nameof(SavePriceListRequest.Name)));
    }

    [Fact]
    public void Categoria_PadreCeroEsInvalido()
    {
        Assert.True(HasErrorFor(Validate(new SaveCategoryRequest { Name = "Novela", ParentId = 0 }), nameof(SaveCategoryRequest.ParentId)));
    }

    [Theory]
    [InlineData("Ciencia ficción", "CIENCIA_FICCION")]
    [InlineData("Arte & diseño (gráfico)", "ARTE_DISENO_GRAFICO")]
    [InlineData("  Niños 0-5 años ", "NINOS_0_5_ANOS")]
    public void Codigo_DesdeNombre_SinTildesNiSignos(string name, string expected)
    {
        Assert.Equal(expected, CatalogCodes.FromName(name, 30));
    }

    [Fact]
    public void Codigo_DesdeNombre_RespetaLongitudYSeVuelveUnico()
    {
        string code = CatalogCodes.FromName("Precio especial para distribuidores", 20);

        Assert.Equal("PRECIO_ESPECIAL_PARA", code);
        Assert.Equal("PRECIO_ESPECIAL_PA_2", CatalogCodes.MakeUnique(code, 20, ["precio_especial_para"]));
    }

    [Fact]
    public void Codigo_Correlativo_IgnoraCodigosConOtroFormato()
    {
        Assert.Equal("SUC-03", CatalogCodes.NextSequential("SUC-", ["SUC-01", "SUC-02", "SUC-X9", "CV123"]));
        Assert.Equal("CAJA-01", CatalogCodes.NextSequential("CAJA-", []));
    }
}
