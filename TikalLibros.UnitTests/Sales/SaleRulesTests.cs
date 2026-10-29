using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Sales;

namespace TikalLibros.UnitTests.Sales;

public sealed class SaleRulesTests
{
    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("4242", true)]
    [InlineData("424", false)]
    [InlineData("42a2", false)]
    [InlineData("42424", false)]
    public void Pago_UltimosCuatroDigitosSoloNumeros(string digits, bool valid)
    {
        PaymentRequest payment = new() { PaymentMethodId = 1, Amount = 10m, LastFourDigits = digits };

        Assert.Equal(valid, Validate(payment).Count == 0);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0.01, true)]
    [InlineData(10.555, false)]
    [InlineData(1000000.01, false)]
    public void Pago_MontoPositivoConDosDecimales(double amount, bool valid)
    {
        PaymentRequest payment = new() { PaymentMethodId = 1, Amount = (decimal)amount };

        Assert.Equal(valid, Validate(payment).Count == 0);
    }

    [Theory]
    [InlineData("TRF-2026-01", true)]
    [InlineData("TRF 01", false)]
    [InlineData("<script>", false)]
    public void Pago_ReferenciaSinEspaciosNiSignos(string reference, bool valid)
    {
        PaymentRequest payment = new() { PaymentMethodId = 1, Amount = 10m, Reference = reference };

        Assert.Equal(valid, Validate(payment).Count == 0);
    }

    [Fact]
    public void VentaAlCredito_SinCliente_NoEsValida()
    {
        DirectSaleRequest sale = new()
        {
            SaleType = SaleType.Credit,
            Lines = [new OrderLineRequest { BookId = 1, Quantity = 1 }]
        };

        Assert.Contains(Validate(sale), result => result.MemberNames.Contains(nameof(DirectSaleRequest.CustomerId)));
    }

    [Fact]
    public void VentaAlContado_SinCliente_EsConsumidorFinal()
    {
        DirectSaleRequest sale = new()
        {
            Lines = [new OrderLineRequest { BookId = 1, Quantity = 1 }],
            Payments = [new PaymentRequest { PaymentMethodId = 1, Amount = 10m }]
        };

        Assert.Empty(Validate(sale));
    }

    [Fact]
    public void Abono_SinPagos_NoEsValido()
    {
        Assert.Contains(Validate(new AddPaymentsRequest()), result => result.ErrorMessage == "Indique al menos un pago.");
    }
}
