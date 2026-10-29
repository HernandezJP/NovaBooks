using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Sales;

/// <summary>Códigos de estado de una devolución de cliente.</summary>
public static class ReturnStatus
{
    public const string Processed = "PROCESADA";
    public const string Annulled = "ANULADA";
}

public sealed class ReturnReasonResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>El libro vuelve al inventario (si no, se da de baja: dañado o defectuoso).</summary>
    public bool Restocks { get; set; }

    public bool RequiresNotes { get; set; }
}

public sealed class ReturnCatalogsResponse
{
    public IReadOnlyList<ReturnReasonResponse> Reasons { get; set; } = [];

    /// <summary>Métodos con que se reembolsa lo que el cliente ya había pagado.</summary>
    public IReadOnlyList<CatalogOptionResponse> RefundMethods { get; set; } = [];
}

public sealed class ReturnQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public int? SaleId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public bool IncludeAnnulled { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class ReturnListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int SaleId { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public decimal Units { get; set; }

    public decimal Total { get; set; }

    /// <summary>Parte aplicada al saldo pendiente de la venta al crédito.</summary>
    public decimal CreditApplied { get; set; }

    /// <summary>Parte reembolsada al cliente por caja.</summary>
    public decimal Refunded { get; set; }

    public string Employee { get; set; } = string.Empty;

    public bool Annulled { get; set; }
}

public sealed class ReturnLineRequest
{
    [Required(ErrorMessage = "Seleccione la línea de la venta.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la línea de la venta.")]
    public int? SaleLineId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe ser un número entero entre 1 y 100,000.")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "Seleccione el motivo.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el motivo.")]
    public int? ReasonId { get; set; }

    [StringLength(500, ErrorMessage = "La observación no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }
}

public sealed class SaveReturnRequest : IValidatableObject
{
    public const int MaxLines = 200;

    [Required(ErrorMessage = "Seleccione la venta.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la venta.")]
    public int? SaleId { get; set; }

    /// <summary>Método del reembolso por caja; si no se indica, efectivo.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "El método de reembolso no es válido.")]
    public int? RefundMethodId { get; set; }

    [StringLength(150, ErrorMessage = "La referencia no puede superar los 150 caracteres.")]
    [PlainText]
    public string? RefundReference { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    [UniqueBy(nameof(ReturnLineRequest.SaleLineId), "Cada línea de la venta solo puede aparecer una vez en la devolución.")]
    public List<ReturnLineRequest> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines.Count == 0)
        {
            yield return new ValidationResult("Agregue al menos un libro a la devolución.", [nameof(Lines)]);
        }
        else if (Lines.Count > MaxLines)
        {
            yield return new ValidationResult($"La devolución admite como máximo {MaxLines} líneas.", [nameof(Lines)]);
        }

        foreach (ValidationResult result in NestedValidation.Validate(Lines, nameof(Lines)))
        {
            yield return result;
        }
    }
}

public sealed class ReturnLineResponse
{
    public int SaleLineId { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    /// <summary>Precio unitario con IVA y descuento ya aplicados.</summary>
    public decimal UnitPrice { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool Restocks { get; set; }

    public string? Notes { get; set; }
}

public sealed class ReturnRefundResponse
{
    public string PaymentMethodCode { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Reference { get; set; }

    public string? CashMovementNumber { get; set; }

    public bool Annulled { get; set; }
}

public sealed class ReturnResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int SaleId { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public string? CustomerNit { get; set; }

    public string Warehouse { get; set; } = string.Empty;

    public string? CashRegister { get; set; }

    public string Employee { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public decimal CreditApplied { get; set; }

    public decimal Refunded { get; set; }

    public string? InventoryMovementNumber { get; set; }

    public bool Annulled { get; set; }

    public DateTime? AnnulledAt { get; set; }

    public string? AnnulmentReason { get; set; }

    public bool CanAnnul { get; set; }

    public IReadOnlyList<ReturnLineResponse> Lines { get; set; } = [];

    public IReadOnlyList<ReturnRefundResponse> Refunds { get; set; } = [];
}

/// <summary>Devolución resumida dentro del detalle de la venta.</summary>
public sealed class SaleReturnSummaryResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public decimal Total { get; set; }

    public bool Annulled { get; set; }
}

public sealed class AnnulReturnRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;
}
