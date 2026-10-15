namespace TikalLibros.Infrastructure.Services.Purchasing;

/// <summary>
/// Montos de una línea de compra. El costo unitario no incluye impuesto:
/// subtotal = cantidad × costo; descuento sobre el subtotal; impuesto sobre
/// (subtotal − descuento). Todo redondeado a 2 decimales.
/// </summary>
public readonly record struct LineAmounts(decimal Subtotal, decimal Discount, decimal Tax, decimal Total)
{
    public static LineAmounts Calculate(decimal quantity, decimal unitCost, decimal discountPercent, decimal taxPercent)
    {
        decimal subtotal = Round(quantity * unitCost);
        decimal discount = Round(subtotal * discountPercent / 100m);
        decimal tax = Round((subtotal - discount) * taxPercent / 100m);

        return new LineAmounts(subtotal, discount, tax, subtotal - discount + tax);
    }

    public static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
