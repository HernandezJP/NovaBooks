using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Services.Sales;

namespace TikalLibros.UnitTests.Sales;

public sealed class OrderRulesTests
{
    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(2, 112, 5, 12, 224, 11.20, 22.80, 212.80)]
    [InlineData(1, 100, 0, 12, 100, 0, 10.71, 100)]
    [InlineData(3, 45.50, 10, 0, 136.50, 13.65, 0, 122.85)]
    [InlineData(1, 0.05, 50, 12, 0.05, 0.03, 0, 0.02)]
    public void SaleLineAmounts_PrecioConIvaIncluido(
        int quantity, double price, double discount, double tax,
        double subtotal, double discountAmount, double taxAmount, double total)
    {
        SaleLineAmounts amounts = SaleLineAmounts.Calculate(quantity, (decimal)price, (decimal)discount, (decimal)tax);

        Assert.Equal((decimal)subtotal, amounts.Subtotal);
        Assert.Equal((decimal)discountAmount, amounts.Discount);
        Assert.Equal((decimal)taxAmount, amounts.Tax);
        Assert.Equal((decimal)total, amounts.Total);
        Assert.Equal(amounts.Subtotal - amounts.Discount, amounts.Total);
    }

    [Fact]
    public void Pedido_SinLineas_NoEsValido()
    {
        List<ValidationResult> results = Validate(new SaveOrderRequest { CustomerId = 1 });

        Assert.Contains(results, result => result.ErrorMessage == "Agregue al menos un libro al pedido.");
    }

    [Fact]
    public void Pedido_ConMasDeDoscientosLibros_NoEsValido()
    {
        SaveOrderRequest request = new()
        {
            CustomerId = 1,
            Lines = Enumerable.Range(1, SaveOrderRequest.MaxLines + 1)
                .Select(book => new OrderLineRequest { BookId = book, Quantity = 1 })
                .ToList()
        };

        Assert.Contains(Validate(request), result => result.ErrorMessage!.Contains("como máximo 200 libros"));
    }

    [Theory]
    [InlineData("CONTADO", true)]
    [InlineData("CREDITO", true)]
    [InlineData("credito", false)]
    [InlineData("FIADO", false)]
    public void Pedido_CondicionDePago(string saleType, bool valid)
    {
        SaveOrderRequest request = new()
        {
            CustomerId = 1,
            SaleType = saleType,
            Lines = [new OrderLineRequest { BookId = 1, Quantity = 1 }]
        };

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(10.5, 1, true)]
    [InlineData(100, 1, true)]
    [InlineData(100.01, 1, false)]
    [InlineData(-1, 1, false)]
    [InlineData(5.125, 1, false)]
    [InlineData(5, 0, false)]
    [InlineData(5, 10001, false)]
    public void LineaDePedido_DescuentoYCantidad(double discount, int quantity, bool valid)
    {
        OrderLineRequest line = new() { BookId = 1, Quantity = quantity, DiscountPercent = (decimal)discount };

        Assert.Equal(valid, Validate(line).Count == 0);
    }

    [Fact]
    public void MotivoDeRechazo_EsObligatorioYSinEtiquetas()
    {
        Assert.NotEmpty(Validate(new OrderReasonRequest { Reason = "no" }));
        Assert.NotEmpty(Validate(new OrderReasonRequest { Reason = "<b>descuento</b>" }));
        Assert.Empty(Validate(new OrderReasonRequest { Reason = "Descuento fuera de política" }));
    }
}
