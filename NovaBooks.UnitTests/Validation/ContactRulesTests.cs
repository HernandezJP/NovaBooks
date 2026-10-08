using NovaBooks.Application.Validation;

namespace NovaBooks.UnitTests.Validation;

public sealed class ContactRulesTests
{
    [Theory]
    [InlineData("5555-1234", "55551234")]
    [InlineData("(502) 5555 1234", "50255551234")]
    [InlineData("+502 5555.1234", "+50255551234")]
    public void NormalizePhone_QuitaSeparadores(string input, string expected)
    {
        Assert.Equal(expected, ContactRules.NormalizePhone(input));
    }

    [Theory]
    [InlineData("55551234", true)]
    [InlineData("+50255551234", true)]
    [InlineData("5555123", false)]          // menos de 8 dígitos
    [InlineData("1234567890123456", false)] // más de 15 dígitos
    [InlineData("5555ABCD", false)]
    public void IsValidPhone_ValidaLongitudYDigitos(string phone, bool expected)
    {
        Assert.Equal(expected, ContactRules.IsValidPhone(phone));
    }

    [Theory]
    [InlineData("1234567-K", "1234567K")]
    [InlineData(" 12345 67-8 ", "12345678")]
    public void NormalizeIdentification_QuitaGuionesYEspaciosYPoneMayusculas(string input, string expected)
    {
        Assert.Equal(expected, ContactRules.NormalizeIdentification(input));
    }

    [Theory]
    [InlineData("12345678", true)]
    [InlineData("1234567K", true)]
    [InlineData("K1234567", false)]   // la K solo puede ser el verificador
    [InlineData("12345A78", false)]
    public void IsValidNitFormat_AceptaDigitosConVerificadorNumericoOK(string nit, bool expected)
    {
        Assert.Equal(expected, ContactRules.IsValidNitFormat(nit));
    }

    [Theory]
    [InlineData("CLI-000001", 20, true)]
    [InlineData("LIB_2026", 30, true)]
    [InlineData("-CLI", 20, false)]       // no puede iniciar con guion
    [InlineData("CLI 01", 20, false)]     // sin espacios
    [InlineData("ABCDEFGHIJKLMNOPQRSTU", 20, false)] // 21 caracteres
    public void IsValidCode_RespetaFormatoYLongitud(string code, int maxLength, bool expected)
    {
        Assert.Equal(expected, ContactRules.IsValidCode(code, maxLength));
    }

    [Fact]
    public void NormalizeEmail_QuitaEspaciosYPasaAMinusculas()
    {
        Assert.Equal("ana@novabooks.com", ContactRules.NormalizeEmail("  Ana@NovaBooks.com "));
    }
}
