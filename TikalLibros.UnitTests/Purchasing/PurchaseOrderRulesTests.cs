using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Infrastructure.Services.Purchasing;

namespace TikalLibros.UnitTests.Purchasing;

public sealed class PurchaseOrderRulesTests
{
    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(10, 50, 10, 12, 500, 50, 54, 504)]
    [InlineData(3, 33.33, 0, 12, 99.99, 0, 12.00, 111.99)]
    [InlineData(1, 0.05, 50, 12, 0.05, 0.03, 0.00, 0.02)]
    [InlineData(7, 120, 0, 0, 840, 0, 0, 840)]
    public void LineAmounts_CalculaSubtotalDescuentoImpuestoYTotal(
        int quantity, double cost, double discount, double tax,
        double subtotal, double discountAmount, double taxAmount, double total)
    {
        LineAmounts amounts = LineAmounts.Calculate(quantity, (decimal)cost, (decimal)discount, (decimal)tax);

        Assert.Equal((decimal)subtotal, amounts.Subtotal);
        Assert.Equal((decimal)discountAmount, amounts.Discount);
        Assert.Equal((decimal)taxAmount, amounts.Tax);
        Assert.Equal((decimal)total, amounts.Total);
    }

    [Fact]
    public void Orden_Valida_NoTieneErrores()
    {
        SavePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            Lines = [new PurchaseOrderLineRequest { BookId = 1, Quantity = 5, UnitCost = 25.50m }]
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Orden_SinLineas_TieneError()
    {
        Assert.Contains(Validate(new SavePurchaseOrderRequest { SupplierId = 1 }), result => result.MemberNames.Contains("Lines"));
    }

    [Fact]
    public void Orden_LineaInvalida_IndicaLineaYCampo()
    {
        SavePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            Lines =
            [
                new PurchaseOrderLineRequest { BookId = 1, Quantity = 1, UnitCost = 10m },
                new PurchaseOrderLineRequest { BookId = 2, Quantity = 0, UnitCost = 10.123m, DiscountPercent = 101m }
            ]
        };

        List<string> members = Validate(request).SelectMany(result => result.MemberNames).ToList();

        Assert.Contains("Lines[1].Quantity", members);
        Assert.Contains("Lines[1].UnitCost", members);
        Assert.Contains("Lines[1].DiscountPercent", members);
        Assert.DoesNotContain(members, member => member.StartsWith("Lines[0]"));
    }

    [Fact]
    public void Orden_FechaDeEntregaPasada_TieneError()
    {
        SavePurchaseOrderRequest request = new()
        {
            SupplierId = 1,
            ExpectedDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
            Lines = [new PurchaseOrderLineRequest { BookId = 1, Quantity = 1, UnitCost = 10m }]
        };

        Assert.Contains(Validate(request), result => result.MemberNames.Contains(nameof(SavePurchaseOrderRequest.ExpectedDate)));
    }

    [Fact]
    public void Cancelacion_SinMotivoSuficiente_TieneError()
    {
        Assert.NotEmpty(Validate(new CancelPurchaseOrderRequest { Reason = "no" }));
    }
}
