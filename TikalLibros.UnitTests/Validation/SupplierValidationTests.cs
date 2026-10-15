using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Suppliers;

namespace TikalLibros.UnitTests.Validation;

public sealed class SupplierValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string member) =>
        results.Any(result => result.MemberNames.Contains(member));

    [Theory]
    [InlineData("Distribuidora Piedra Santa, S.A.")]
    [InlineData("Libros & Más (Zona 10)")]
    [InlineData("Editorial O'Brien / Centroamérica")]
    [InlineData("3M Guatemala")]
    public void Proveedor_JuridicoValido_NoTieneErrores(string legalName)
    {
        SaveSupplierRequest request = new() { PersonType = "JURIDICA", LegalName = legalName, Nit = "1234567K" };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("Distribuidora #1")]
    [InlineData("Libros@Guate")]
    [InlineData("-Editorial")]
    [InlineData("Ventas $ Libros")]
    public void Proveedor_RazonSocialConSimbolosNoPermitidos_TieneError(string legalName)
    {
        SaveSupplierRequest request = new() { PersonType = "JURIDICA", LegalName = legalName, Nit = "1234567K" };

        Assert.True(HasErrorFor(Validate(request), nameof(SaveSupplierRequest.LegalName)));
    }

    [Fact]
    public void Proveedor_SinNitNiRazonSocial_TieneErrores()
    {
        List<ValidationResult> results = Validate(new SaveSupplierRequest { PersonType = "JURIDICA" });

        Assert.True(HasErrorFor(results, nameof(SaveSupplierRequest.Nit)));
        Assert.True(HasErrorFor(results, nameof(SaveSupplierRequest.LegalName)));
    }

    [Fact]
    public void Proveedor_NaturalConNumerosEnNombre_TieneErrores()
    {
        SaveSupplierRequest request = new()
        {
            PersonType = "NATURAL",
            FirstName = "Carlos3",
            LastName = "Ruiz!",
            Nit = "1234567K",
            Phone = "5555-12a4",
            PhoneExtension = "12a"
        };

        List<ValidationResult> results = Validate(request);

        Assert.True(HasErrorFor(results, nameof(SaveSupplierRequest.FirstName)));
        Assert.True(HasErrorFor(results, nameof(SaveSupplierRequest.LastName)));
        Assert.True(HasErrorFor(results, nameof(SaveSupplierRequest.Phone)));
        Assert.True(HasErrorFor(results, nameof(SaveSupplierRequest.PhoneExtension)));
        Assert.True(HasErrorFor(results, nameof(SaveSupplierRequest.PhoneTypeId)));
    }

    [Fact]
    public void Proveedor_DiasDeCreditoSinLimite_TieneError()
    {
        SaveSupplierRequest request = new() { PersonType = "JURIDICA", LegalName = "Distribuidora", Nit = "1234567K", CreditDays = 30 };

        Assert.True(HasErrorFor(Validate(request), nameof(SaveSupplierRequest.CreditLimit)));
    }
}
