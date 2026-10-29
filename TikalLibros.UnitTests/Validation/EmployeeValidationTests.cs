using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Employees;
using TikalLibros.Application.DTOs.Users;
using TikalLibros.Application.Validation;

namespace TikalLibros.UnitTests.Validation;

public sealed class EmployeeValidationTests
{
    // 1·2 + 2·3 + … + 8·9 = 240; 240 mod 11 = 9 → verificador 9.
    private const string ValidDpi = "1234567890101";

    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string member) =>
        results.Any(result => result.MemberNames.Contains(member));

    private static SaveEmployeeRequest ValidEmployee() => new()
    {
        FirstName = "Ana María",
        LastName = "López",
        Dpi = ValidDpi,
        PositionId = 1,
        BranchId = 1,
        HireDate = DateOnly.FromDateTime(DateTime.Today)
    };

    [Theory]
    [InlineData(ValidDpi, true)]
    [InlineData("1234 56789 0101", true)]
    [InlineData("1234-56789-0101", true)]
    [InlineData("1234567800101", false)] // verificador incorrecto
    [InlineData("1234567892301", false)] // departamento 23 no existe
    [InlineData("1234567890118", false)] // Guatemala solo tiene 17 municipios
    [InlineData("1234567890100", false)] // municipio 00
    [InlineData("123456789010", false)]  // 12 dígitos
    [InlineData("12345678901A1", false)] // letra
    public void Dpi_ValidaFormatoVerificadorDepartamentoYMunicipio(string dpi, bool expected)
    {
        Assert.Equal(expected, DpiRules.IsValid(DpiRules.Normalize(dpi)));
    }

    [Fact]
    public void Empleado_Valido_NoTieneErrores()
    {
        Assert.Empty(Validate(ValidEmployee()));
    }

    [Fact]
    public void Empleado_NombresConNumerosYDpiInvalido_MuestraTodosLosErrores()
    {
        SaveEmployeeRequest request = ValidEmployee();
        request.FirstName = "Ana1";
        request.SecondLastName = "Pérez#";
        request.Dpi = "1234567800101";
        request.Phone = "55A51234";

        List<ValidationResult> results = Validate(request);

        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.FirstName)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.SecondLastName)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.Dpi)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.Phone)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.PhoneTypeId)));
    }

    [Fact]
    public void Empleado_SinCamposObligatorios_TieneErrores()
    {
        List<ValidationResult> results = Validate(new SaveEmployeeRequest());

        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.FirstName)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.LastName)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.Dpi)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.PositionId)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.BranchId)));
        Assert.True(HasErrorFor(results, nameof(SaveEmployeeRequest.HireDate)));
    }

    [Fact]
    public void Empleado_MenorDeEdad_TieneError()
    {
        SaveEmployeeRequest request = ValidEmployee();
        request.BirthDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-17);

        Assert.True(HasErrorFor(Validate(request), nameof(SaveEmployeeRequest.BirthDate)));
    }

    [Fact]
    public void Empleado_FinalizacionAntesDeContratacion_TieneError()
    {
        SaveEmployeeRequest request = ValidEmployee();
        request.EndDate = request.HireDate!.Value.AddDays(-1);

        Assert.True(HasErrorFor(Validate(request), nameof(SaveEmployeeRequest.EndDate)));
    }

    [Theory]
    [InlineData("jlopez", true)]
    [InlineData("ana.lopez_2", true)]
    [InlineData("j-lopez", true)]
    [InlineData("josé", false)]
    [InlineData("ana lopez", false)]
    [InlineData("ana@lopez", false)]
    [InlineData(".ana", false)]
    public void Usuario_NombreDeUsuarioSoloCaracteresPermitidos(string userName, bool expected)
    {
        CreateUserRequest request = new()
        {
            UserName = userName,
            Email = "ana@tikal.test",
            Password = "Clave123*"
        };

        Assert.Equal(expected, !HasErrorFor(Validate(request), nameof(CreateUserRequest.UserName)));
    }
}
