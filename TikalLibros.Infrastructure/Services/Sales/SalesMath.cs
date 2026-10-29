using TikalLibros.Infrastructure.Services.Purchasing;

namespace TikalLibros.Infrastructure.Services.Sales;

/// <summary>
/// Montos de una línea de pedido o venta. El precio de lista es el precio al
/// público e incluye IVA: subtotal = cantidad × precio; el descuento se
/// aplica sobre el subtotal; total = subtotal − descuento; el IVA es la parte
/// del total que corresponde al impuesto (total − total ÷ (1 + tasa)).
/// Todo redondeado a 2 decimales.
/// </summary>
public readonly record struct SaleLineAmounts(decimal Subtotal, decimal Discount, decimal Tax, decimal Total)
{
    public static SaleLineAmounts Calculate(decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent)
    {
        decimal subtotal = LineAmounts.Round(quantity * unitPrice);
        decimal discount = LineAmounts.Round(subtotal * discountPercent / 100m);
        decimal total = subtotal - discount;
        decimal tax = LineAmounts.Round(total - total / (1m + taxPercent / 100m));

        return new SaleLineAmounts(subtotal, discount, tax, total);
    }
}
