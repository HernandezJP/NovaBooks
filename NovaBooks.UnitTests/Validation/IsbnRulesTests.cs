using NovaBooks.Application.Validation;

namespace NovaBooks.UnitTests.Validation;

public sealed class IsbnRulesTests
{
    [Theory]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData(" 978 0306 406157 ", "9780306406157")]
    [InlineData("0-8044-2957-x", "080442957X")]
    public void Normalize_QuitaGuionesYEspacios(string input, string expected)
    {
        Assert.Equal(expected, IsbnRules.Normalize(input));
    }

    [Theory]
    [InlineData("0306406152")]
    [InlineData("080442957X")]
    public void IsValidIsbn10_AceptaDigitoDeControlCorrecto(string isbn)
    {
        Assert.True(IsbnRules.IsValidIsbn10(isbn));
    }

    [Theory]
    [InlineData("0306406153")]  // dígito de control incorrecto
    [InlineData("030640615")]   // longitud incorrecta
    [InlineData("03064061X2")]  // X fuera de la última posición
    [InlineData("ABCDEFGHIJ")]
    public void IsValidIsbn10_RechazaValoresInvalidos(string isbn)
    {
        Assert.False(IsbnRules.IsValidIsbn10(isbn));
    }

    [Theory]
    [InlineData("9780306406157")]
    [InlineData("9789992200117")]
    public void IsValidIsbn13_AceptaDigitoDeControlCorrecto(string isbn)
    {
        Assert.True(IsbnRules.IsValidIsbn13(isbn));
    }

    [Theory]
    [InlineData("9780306406158")]  // dígito de control incorrecto
    [InlineData("9770306406157")]  // prefijo distinto de 978/979
    [InlineData("978030640615")]   // longitud incorrecta
    [InlineData("978030640615X")]
    public void IsValidIsbn13_RechazaValoresInvalidos(string isbn)
    {
        Assert.False(IsbnRules.IsValidIsbn13(isbn));
    }

    [Fact]
    public void ToIsbn13_ConvierteIsbn10EquivalenteConPrefijo978()
    {
        Assert.Equal("9780306406157", IsbnRules.ToIsbn13("0306406152"));
    }
}
