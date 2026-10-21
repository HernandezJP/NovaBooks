using TikalLibros.Application.DTOs.Sales;

namespace TikalLibros.Web.Components.Shared.Sales;

/// <summary>Montos de una línea; mismo cálculo que la API (precio con IVA incluido).</summary>
public readonly record struct SaleAmounts(decimal Subtotal, decimal Discount, decimal Tax, decimal Total)
{
    public static SaleAmounts Calculate(decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent)
    {
        decimal subtotal = Round(quantity * unitPrice);
        decimal discount = Round(subtotal * discountPercent / 100m);
        decimal total = subtotal - discount;
        decimal tax = Round(total - total / (1m + taxPercent / 100m));

        return new SaleAmounts(subtotal, discount, tax, total);
    }

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Línea editable de pedido o venta.</summary>
public sealed class SaleLineRow
{
    public int BookId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Isbn { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TaxPercent { get; init; }

    /// <summary>Existencia libre al agregarla (informativa; la API vuelve a validar).</summary>
    public decimal Available { get; set; }

    public int? Quantity { get; set; } = 1;

    public decimal DiscountPercent { get; set; }

    public SaleAmounts Amounts => SaleAmounts.Calculate(Quantity ?? 0, UnitPrice, DiscountPercent, TaxPercent);

    public string? QuantityError => Quantity is null or < 1 or > 10000 ? "La cantidad debe ser un entero entre 1 y 10,000." : null;

    public string? DiscountError(decimal maximum) =>
        DiscountPercent < 0 || DiscountPercent > maximum ? $"El descuento debe estar entre 0 % y {maximum:0.##} %."
        : decimal.Round(DiscountPercent, 2) != DiscountPercent ? "El descuento admite como máximo 2 decimales."
        : null;

    public string? Error(decimal maximumDiscount) => QuantityError ?? DiscountError(maximumDiscount);

    public OrderLineRequest ToRequest() => new()
    {
        BookId = BookId,
        Quantity = Quantity,
        DiscountPercent = DiscountPercent
    };

    public static SaleLineRow From(SalesBookOptionResponse book) => new()
    {
        BookId = book.Id,
        Code = book.Code,
        Title = book.Title,
        Isbn = book.Isbn13,
        UnitPrice = book.Price ?? 0m,
        TaxPercent = book.TaxPercent,
        Available = book.Available
    };

    public static SaleLineRow From(OrderLineResponse line) => new()
    {
        BookId = line.BookId,
        Code = line.BookCode,
        Title = line.Title,
        UnitPrice = line.UnitPrice,
        TaxPercent = line.TaxPercent,
        Available = line.Available + line.Reserved,
        Quantity = (int)line.Quantity,
        DiscountPercent = line.DiscountPercent
    };
}

/// <summary>Pago editable.</summary>
public sealed class PaymentRow
{
    public PaymentMethodResponse? Method { get; set; }

    public decimal? Amount { get; set; }

    /// <summary>Solo efectivo: lo que entregó el cliente.</summary>
    public decimal? Received { get; set; }

    public string? Reference { get; set; }

    public string? Authorization { get; set; }

    public string? LastFourDigits { get; set; }

    public bool IsCash => Method?.AllowsChange == true;

    public bool IsCard => Method?.Code == PaymentMethodCode.Card;

    public decimal Change => IsCash && Received is { } received && Amount is { } amount && received > amount ? received - amount : 0m;

    public string? Error =>
        Method is null ? "Seleccione el método de pago."
        : Amount is null or <= 0 ? "Indique el monto del pago."
        : decimal.Round(Amount.Value, 2) != Amount.Value ? "El monto admite como máximo 2 decimales."
        : IsCash && Received is { } received && received < Amount ? "Lo recibido es menor que el monto."
        : IsCard && (LastFourDigits?.Length ?? 0) != 4 ? "Indique los últimos 4 dígitos de la tarjeta."
        : !IsCard && Method.RequiresReference && string.IsNullOrWhiteSpace(Reference) ? "Indique la referencia."
        : Method.RequiresAuthorization && string.IsNullOrWhiteSpace(Authorization) ? "Indique el número de autorización."
        : null;

    public PaymentRequest ToRequest() => new()
    {
        PaymentMethodId = Method?.Id,
        Amount = Amount,
        Received = IsCash ? Received : null,
        Reference = string.IsNullOrWhiteSpace(Reference) ? null : Reference.Trim(),
        Authorization = string.IsNullOrWhiteSpace(Authorization) ? null : Authorization.Trim(),
        LastFourDigits = IsCard && !string.IsNullOrWhiteSpace(LastFourDigits) ? LastFourDigits.Trim() : null
    };
}

/// <summary>Validación de un conjunto de pagos (la misma que muestra el editor).</summary>
public static class PaymentRules
{
    public static string? Error(IReadOnlyCollection<PaymentRow> payments, decimal due, bool requireFull)
    {
        decimal paid = payments.Sum(payment => payment.Amount ?? 0m);

        return payments.Any(payment => payment.Error is not null) ? "Revise los datos de los pagos."
            : paid > due ? "Los pagos superan lo que se cobra; para dar cambio indique lo recibido en efectivo."
            : requireFull && paid < due ? $"Faltan Q {due - paid:N2} por cobrar."
            : null;
    }
}
