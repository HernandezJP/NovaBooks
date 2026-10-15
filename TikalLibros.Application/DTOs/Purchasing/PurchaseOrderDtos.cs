using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Purchasing;

/// <summary>Códigos de estado de una orden de compra.</summary>
public static class PurchaseOrderStatus
{
    public const string Draft = "BORRADOR";
    public const string Issued = "EMITIDA";
    public const string Partial = "PARCIAL";
    public const string Received = "RECIBIDA";
    public const string Cancelled = "CANCELADA";
}

public sealed class PurchaseOrderListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    public string CurrencySymbol { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int Lines { get; set; }

    public decimal OrderedUnits { get; set; }

    public decimal ReceivedUnits { get; set; }
}

public sealed class PurchaseOrderLineResponse
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Isbn { get; set; }

    public decimal Quantity { get; set; }

    public decimal Received { get; set; }

    public decimal Pending { get; set; }

    public decimal UnitCost { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxPercent { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public string? Notes { get; set; }
}

public sealed class PurchaseOrderResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string? SupplierNit { get; set; }

    public int BranchId { get; set; }

    public string Branch { get; set; } = string.Empty;

    public int CurrencyId { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string CurrencySymbol { get; set; } = string.Empty;

    public decimal ExchangeRate { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    public string? RequestedBy { get; set; }

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public IReadOnlyCollection<PurchaseOrderLineResponse> Lines { get; set; } = [];

    public bool CanEdit { get; set; }

    public bool CanIssue { get; set; }

    public bool CanCancel { get; set; }

    public bool CanReceive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public sealed class PurchaseOrderLineRequest
{
    [Required(ErrorMessage = "Seleccione el libro.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el libro.")]
    public int? BookId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe ser un número entero entre 1 y 100,000.")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "El costo unitario es obligatorio.")]
    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "El costo unitario debe estar entre 0.01 y 999,999.99.")]
    [MaxDecimals(2, "El costo unitario admite como máximo 2 decimales.")]
    public decimal? UnitCost { get; set; }

    [Range(typeof(decimal), "0", "100", ErrorMessage = "El descuento debe estar entre 0 % y 100 %.")]
    [MaxDecimals(2, "El descuento admite como máximo 2 decimales.")]
    public decimal DiscountPercent { get; set; }

    [StringLength(500, ErrorMessage = "La nota de la línea no puede superar los 500 caracteres.")]
    public string? Notes { get; set; }
}

public sealed class SavePurchaseOrderRequest : IValidatableObject
{
    public const int MaxLines = 200;

    [Required(ErrorMessage = "Seleccione el proveedor.")]
    public int? SupplierId { get; set; }

    /// <summary>Si no se indica, la sucursal principal.</summary>
    public int? BranchId { get; set; }

    /// <summary>Si no se indica, la moneda predeterminada.</summary>
    public int? CurrencyId { get; set; }

    /// <summary>Obligatorio para monedas distintas de la predeterminada.</summary>
    [Range(typeof(decimal), "0.000001", "100000", ErrorMessage = "El tipo de cambio debe ser mayor que 0.")]
    [MaxDecimals(6, "El tipo de cambio admite como máximo 6 decimales.")]
    public decimal? ExchangeRate { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    public string? Notes { get; set; }

    [UniqueBy(nameof(PurchaseOrderLineRequest.BookId), "Cada libro solo puede aparecer una vez en la orden.")]
    public List<PurchaseOrderLineRequest> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines.Count == 0)
        {
            yield return new ValidationResult("Agregue al menos un libro a la orden.", [nameof(Lines)]);
        }
        else if (Lines.Count > MaxLines)
        {
            yield return new ValidationResult($"La orden admite como máximo {MaxLines} libros.", [nameof(Lines)]);
        }

        for (int index = 0; index < Lines.Count; index++)
        {
            List<ValidationResult> lineResults = [];

            if (Lines[index] is { } line &&
                !Validator.TryValidateObject(line, new ValidationContext(line), lineResults, validateAllProperties: true))
            {
                foreach (ValidationResult result in lineResults)
                {
                    yield return new ValidationResult(
                        $"Línea {index + 1}: {result.ErrorMessage}",
                        result.MemberNames.Select(member => $"{nameof(Lines)}[{index}].{member}").ToArray());
                }
            }
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        if (ExpectedDate.HasValue && (ExpectedDate.Value < today || ExpectedDate.Value > today.AddYears(1)))
        {
            yield return new ValidationResult(
                "La fecha de entrega esperada debe estar entre hoy y un año.",
                [nameof(ExpectedDate)]);
        }
    }
}

public sealed class CancelPurchaseOrderRequest
{
    [Required(ErrorMessage = "Indique el motivo de la cancelación.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class PurchaseOrderQuery
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public int? SupplierId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class PurchaseBookOptionResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Isbn { get; set; }

    public decimal TaxPercent { get; set; }

    /// <summary>Costo de la última orden no cancelada que incluyó el libro.</summary>
    public decimal? LastCost { get; set; }
}

public sealed class CurrencyOptionResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public sealed class PurchaseOrderCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> Suppliers { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> Branches { get; set; } = [];

    public IReadOnlyCollection<CurrencyOptionResponse> Currencies { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> Statuses { get; set; } = [];
}
