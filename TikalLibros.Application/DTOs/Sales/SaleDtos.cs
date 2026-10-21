using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Sales;

/// <summary>
/// Un pago de la venta. En efectivo puede indicarse lo recibido para
/// calcular el cambio; tarjeta exige autorización y últimos 4 dígitos;
/// transferencia exige la referencia.
/// </summary>
public sealed class PaymentRequest
{
    [Required(ErrorMessage = "Seleccione el método de pago.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el método de pago.")]
    public int? PaymentMethodId { get; set; }

    /// <summary>Lo que se abona a la venta.</summary>
    [Required(ErrorMessage = "Indique el monto del pago.")]
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "El monto del pago debe estar entre 0.01 y 1,000,000.")]
    [MaxDecimals(2, "El monto del pago admite como máximo 2 decimales.")]
    public decimal? Amount { get; set; }

    /// <summary>Solo efectivo: lo que entregó el cliente (si es mayor, se da cambio).</summary>
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "El monto recibido debe estar entre 0 y 1,000,000.")]
    [MaxDecimals(2, "El monto recibido admite como máximo 2 decimales.")]
    public decimal? Received { get; set; }

    [StringLength(50, ErrorMessage = "La referencia no puede superar los 50 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "La referencia solo admite letras sin tildes, números y guion.")]
    public string? Reference { get; set; }

    [StringLength(20, ErrorMessage = "La autorización no puede superar los 20 caracteres.")]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "La autorización solo admite letras sin tildes y números.")]
    public string? Authorization { get; set; }

    [RegularExpression(@"^\d{4}$", ErrorMessage = "Indique los últimos 4 dígitos de la tarjeta.")]
    public string? LastFourDigits { get; set; }
}

/// <summary>Facturar un pedido reservado.</summary>
public sealed class InvoiceOrderRequest : IValidatableObject
{
    public List<PaymentRequest> Payments { get; set; } = [];

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        NestedValidation.Validate(Payments, nameof(Payments));
}

/// <summary>Venta de mostrador, sin pedido previo.</summary>
public sealed class DirectSaleRequest : IValidatableObject
{
    /// <summary>Opcional al contado (consumidor final); obligatorio al crédito.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el cliente.")]
    public int? CustomerId { get; set; }

    [RegularExpression("^(CONTADO|CREDITO)$", ErrorMessage = "La condición de pago debe ser contado o crédito.")]
    public string SaleType { get; set; } = global::TikalLibros.Application.DTOs.Sales.SaleType.Cash;

    public int? PriceListId { get; set; }

    public int? WarehouseId { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    [UniqueBy(nameof(OrderLineRequest.BookId), "Cada libro solo puede aparecer una vez en la venta.")]
    public List<OrderLineRequest> Lines { get; set; } = [];

    public List<PaymentRequest> Payments { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines.Count == 0)
        {
            yield return new ValidationResult("Agregue al menos un libro a la venta.", [nameof(Lines)]);
        }
        else if (Lines.Count > SaveOrderRequest.MaxLines)
        {
            yield return new ValidationResult($"La venta admite como máximo {SaveOrderRequest.MaxLines} libros.", [nameof(Lines)]);
        }

        if (SaleType == global::TikalLibros.Application.DTOs.Sales.SaleType.Credit && CustomerId is null)
        {
            yield return new ValidationResult("Seleccione el cliente: las ventas al crédito requieren un cliente.", [nameof(CustomerId)]);
        }

        foreach (ValidationResult result in NestedValidation.Validate(Lines, nameof(Lines)))
        {
            yield return result;
        }

        foreach (ValidationResult result in NestedValidation.Validate(Payments, nameof(Payments)))
        {
            yield return result;
        }
    }
}

/// <summary>Abono a una venta al crédito o pagada parcialmente.</summary>
public sealed class AddPaymentsRequest : IValidatableObject
{
    public List<PaymentRequest> Payments { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Payments.Count == 0)
        {
            yield return new ValidationResult("Indique al menos un pago.", [nameof(Payments)]);
        }

        foreach (ValidationResult result in NestedValidation.Validate(Payments, nameof(Payments)))
        {
            yield return result;
        }
    }
}

public sealed class AnnulSaleRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;
}

public sealed class SaleListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int? CustomerId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string SaleTypeCode { get; set; } = string.Empty;

    public string SaleType { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public decimal Paid { get; set; }

    public decimal Balance { get; set; }

    public string? OrderNumber { get; set; }

    public string Employee { get; set; } = string.Empty;

    public bool Annulled { get; set; }
}

public sealed class SaleLineResponse
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    /// <summary>Costo de salida (PEPS); solo con Inventario.VerCostos.</summary>
    public decimal? UnitCost { get; set; }
}

public sealed class SalePaymentResponse
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string PaymentMethodCode { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal Received { get; set; }

    public decimal Change { get; set; }

    public string? Reference { get; set; }

    public string? Authorization { get; set; }

    public string? LastFourDigits { get; set; }

    public string? CashMovementNumber { get; set; }

    public bool Annulled { get; set; }
}

public sealed class SaleResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int? CustomerId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string? CustomerNit { get; set; }

    public int? OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public string Warehouse { get; set; } = string.Empty;

    public string? CashRegister { get; set; }

    public string SaleTypeCode { get; set; } = string.Empty;

    public string SaleType { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Employee { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    /// <summary>IVA incluido en el total.</summary>
    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public decimal Paid { get; set; }

    public decimal Balance { get; set; }

    /// <summary>Cambio entregado en efectivo.</summary>
    public decimal Change { get; set; }

    public DateOnly? CreditDueDate { get; set; }

    public string? InventoryMovementNumber { get; set; }

    public bool Annulled { get; set; }

    public DateTime? AnnulledAt { get; set; }

    public string? AnnulmentReason { get; set; }

    public IReadOnlyCollection<SaleLineResponse> Lines { get; set; } = [];

    public IReadOnlyCollection<SalePaymentResponse> Payments { get; set; } = [];

    public bool CanPay { get; set; }

    public bool CanAnnul { get; set; }
}

/// <summary>Búsqueda avanzada de ventas: todos los filtros se combinan.</summary>
public sealed class SaleQuery
{
    /// <summary>Número de venta o de pedido, código, nombre o NIT del cliente.</summary>
    public string? Search { get; set; }

    public string? Status { get; set; }

    public string? SaleType { get; set; }

    public int? CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    public int? BookId { get; set; }

    public int? PaymentMethodId { get; set; }

    /// <summary>Solo ventas con saldo pendiente.</summary>
    public bool? WithBalance { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public decimal? MinTotal { get; set; }

    public decimal? MaxTotal { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class SaleCatalogsResponse
{
    public IReadOnlyCollection<PaymentMethodResponse> PaymentMethods { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> Statuses { get; set; } = [];
}

public sealed class PaymentMethodResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool RequiresReference { get; set; }

    public bool RequiresAuthorization { get; set; }

    public bool AllowsChange { get; set; }
}
