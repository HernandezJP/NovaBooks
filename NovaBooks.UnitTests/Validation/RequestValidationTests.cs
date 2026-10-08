using System.ComponentModel.DataAnnotations;
using NovaBooks.Application.Authentication;
using NovaBooks.Application.DTOs.Authors;
using NovaBooks.Application.DTOs.Customers;
using NovaBooks.Application.DTOs.Editorials;
using NovaBooks.Application.DTOs.Products;

namespace NovaBooks.UnitTests.Validation;

/// <summary>
/// Validaciones de los modelos de entrada (las mismas que aplican la API
/// con [ApiController] y la Web con DataAnnotationsValidator).
/// </summary>
public sealed class RequestValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string member) =>
        results.Any(result => result.MemberNames.Contains(member));

    // ---------- Clientes ----------

    [Fact]
    public void Cliente_NaturalValido_NoTieneErrores()
    {
        SaveCustomerRequest request = new()
        {
            PersonType = SaveCustomerRequest.NaturalPerson,
            FirstName = "Ana",
            LastName = "López",
            Nit = "1234567-K",
            Phone = "5555-1234",
            PhoneTypeId = 1,
            CreditLimit = 500m,
            CreditDays = 30
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Cliente_Natural_RequiereNombreYApellido()
    {
        List<ValidationResult> results = Validate(new SaveCustomerRequest { PersonType = SaveCustomerRequest.NaturalPerson });

        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.FirstName)));
        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.LastName)));
    }

    [Fact]
    public void Cliente_Juridico_RequiereRazonSocialYNit()
    {
        List<ValidationResult> results = Validate(new SaveCustomerRequest { PersonType = SaveCustomerRequest.LegalPerson });

        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.LegalName)));
        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.Nit)));
    }

    [Fact]
    public void Cliente_TelefonoSinTipoONitInvalido_TieneErrores()
    {
        SaveCustomerRequest request = new()
        {
            PersonType = SaveCustomerRequest.NaturalPerson,
            FirstName = "Ana",
            LastName = "López",
            Nit = "K12345",
            Phone = "123"
        };

        List<ValidationResult> results = Validate(request);

        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.Nit)));
        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.Phone)));
        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.PhoneTypeId)));
    }

    [Fact]
    public void Cliente_CreditoNegativo_TieneError()
    {
        SaveCustomerRequest request = new()
        {
            PersonType = SaveCustomerRequest.NaturalPerson,
            FirstName = "Ana",
            LastName = "López",
            CreditLimit = -1m,
            CreditDays = 400
        };

        List<ValidationResult> results = Validate(request);

        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.CreditLimit)));
        Assert.True(HasErrorFor(results, nameof(SaveCustomerRequest.CreditDays)));
    }

    // ---------- Libros ----------

    private static SaveProductRequest ValidBook() => new()
    {
        Title = "Cien años de soledad",
        LanguageId = 1,
        FormatId = 1,
        TaxId = 1,
        Isbn13 = "978-0-306-40615-7",
        Isbn10 = "0-306-40615-2"
    };

    [Fact]
    public void Libro_Valido_NoTieneErrores()
    {
        Assert.Empty(Validate(ValidBook()));
    }

    [Fact]
    public void Libro_IsbnConDigitoIncorrecto_TieneError()
    {
        SaveProductRequest request = ValidBook();
        request.Isbn13 = "9780306406158";
        request.Isbn10 = null;

        Assert.True(HasErrorFor(Validate(request), nameof(SaveProductRequest.Isbn13)));
    }

    [Fact]
    public void Libro_Isbn10EIsbn13DeLibrosDistintos_TieneError()
    {
        SaveProductRequest request = ValidBook();
        request.Isbn10 = "080442957X";

        Assert.True(HasErrorFor(Validate(request), nameof(SaveProductRequest.Isbn13)));
    }

    [Fact]
    public void Libro_SinTituloNiCatalogos_TieneErrores()
    {
        List<ValidationResult> results = Validate(new SaveProductRequest());

        Assert.True(HasErrorFor(results, nameof(SaveProductRequest.Title)));
        Assert.True(HasErrorFor(results, nameof(SaveProductRequest.LanguageId)));
        Assert.True(HasErrorFor(results, nameof(SaveProductRequest.FormatId)));
        Assert.True(HasErrorFor(results, nameof(SaveProductRequest.TaxId)));
    }

    [Fact]
    public void Libro_CategoriaPrincipalFueraDeLaLista_TieneError()
    {
        SaveProductRequest request = ValidBook();
        request.CategoryIds = [1, 2];
        request.PrimaryCategoryId = 3;

        Assert.True(HasErrorFor(Validate(request), nameof(SaveProductRequest.PrimaryCategoryId)));
    }

    [Fact]
    public void Libro_PrecioNegativoOListaRepetida_TieneErrores()
    {
        SaveProductRequest request = ValidBook();
        request.ReferenceCost = -5m;
        request.Prices = [new() { PriceListId = 1, Price = 10m }, new() { PriceListId = 1, Price = 12m }];

        List<ValidationResult> results = Validate(request);

        Assert.True(HasErrorFor(results, nameof(SaveProductRequest.ReferenceCost)));
        Assert.True(HasErrorFor(results, nameof(SaveProductRequest.Prices)));
    }

    [Fact]
    public void Libro_ChangesPricing_SoloSiSeEnvianPreciosOCosto()
    {
        SaveProductRequest request = ValidBook();
        Assert.False(request.ChangesPricing);

        request.ReferenceCost = 10m;
        Assert.True(request.ChangesPricing);
    }

    // ---------- Autores y editoriales ----------

    [Fact]
    public void Autor_FallecimientoAntesDelNacimiento_TieneError()
    {
        SaveAuthorRequest request = new()
        {
            FirstName = "Gabriel",
            LastName = "García Márquez",
            BirthDate = new DateOnly(1927, 3, 6),
            DeathDate = new DateOnly(1900, 1, 1)
        };

        Assert.True(HasErrorFor(Validate(request), nameof(SaveAuthorRequest.DeathDate)));
    }

    [Fact]
    public void Autor_SinNombreNiApellido_TieneErrores()
    {
        List<ValidationResult> results = Validate(new SaveAuthorRequest());

        Assert.True(HasErrorFor(results, nameof(SaveAuthorRequest.FirstName)));
        Assert.True(HasErrorFor(results, nameof(SaveAuthorRequest.LastName)));
    }

    [Fact]
    public void Editorial_CorreoSitioYTelefonoInvalidos_TieneErrores()
    {
        SaveEditorialRequest request = new()
        {
            Name = "Planeta",
            Email = "no-es-correo",
            Website = "planeta",
            Phone = "12"
        };

        List<ValidationResult> results = Validate(request);

        Assert.True(HasErrorFor(results, nameof(SaveEditorialRequest.Email)));
        Assert.True(HasErrorFor(results, nameof(SaveEditorialRequest.Website)));
        Assert.True(HasErrorFor(results, nameof(SaveEditorialRequest.Phone)));
    }

    // ---------- Login ----------

    [Fact]
    public void Login_SinCredenciales_TieneErrores()
    {
        List<ValidationResult> results = Validate(new LoginRequest());

        Assert.True(HasErrorFor(results, nameof(LoginRequest.UserNameOrEmail)));
        Assert.True(HasErrorFor(results, nameof(LoginRequest.Password)));
    }
}
