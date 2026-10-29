using TikalLibros.Application.Common;

namespace TikalLibros.UnitTests.Sales;

public sealed class AmountInWordsTests
{
    [Theory]
    [InlineData(212.80, "Doscientos doce quetzales con 80/100")]
    [InlineData(1, "Un quetzal con 00/100")]
    [InlineData(21, "Veintiún quetzales con 00/100")]
    [InlineData(31, "Treinta y un quetzales con 00/100")]
    [InlineData(100, "Cien quetzales con 00/100")]
    [InlineData(115, "Ciento quince quetzales con 00/100")]
    [InlineData(0.5, "Cero quetzales con 50/100")]
    [InlineData(1001.5, "Mil un quetzales con 50/100")]
    [InlineData(21000, "Veintiún mil quetzales con 00/100")]
    [InlineData(1250000.75, "Un millón doscientos cincuenta mil quetzales con 75/100")]
    [InlineData(3456.99, "Tres mil cuatrocientos cincuenta y seis quetzales con 99/100")]
    public void Quetzales_EnLetras(double amount, string expected)
    {
        Assert.Equal(expected, AmountInWords.Quetzales((decimal)amount));
    }

    [Fact]
    public void Quetzales_FueraDeRango_Lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmountInWords.Quetzales(-1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => AmountInWords.Quetzales(1_000_000_000m));
    }
}
