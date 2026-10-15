using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Purchasing;

/// <summary>Códigos de estado de una recepción.</summary>
public static class ReceptionStatus
{
    public const string Processed = "PROCESADA";
    public const string Annulled = "ANULADA";
}

public sealed class ReceptionListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int PurchaseOrderId { get; set; }

    public string PurchaseOrderNumber { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public string? SupplierDocument { get; set; }

    public decimal AcceptedUnits { get; set; }

    public decimal RejectedUnits { get; set; }

    /// <summary>Costo de lo aceptado en moneda local.</summary>
    public decimal TotalCost { get; set; }
}

public sealed class ReceptionLineResponse
{
    public int Id { get; set; }

    public int PurchaseOrderLineId { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal Received { get; set; }

    public decimal Accepted { get; set; }

    public decimal Rejected { get; set; }

    /// <summary>Costo unitario neto (sin IVA, con descuento) en moneda local.</summary>
    public decimal UnitCost { get; set; }

    public string? RejectReason { get; set; }

    public string? LotNumber { get; set; }

    public string? Notes { get; set; }
}

public sealed class ReceptionResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int PurchaseOrderId { get; set; }

    public string PurchaseOrderNumber { get; set; } = string.Empty;

    public string PurchaseOrderStatus { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public int WarehouseId { get; set; }

    public string Warehouse { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public string? SupplierDocumentSeries { get; set; }

    public string? SupplierDocumentNumber { get; set; }

    public string ReceivedBy { get; set; } = string.Empty;

    public string? MovementNumber { get; set; }

    public string? Notes { get; set; }

    public decimal TotalCost { get; set; }

    public IReadOnlyCollection<ReceptionLineResponse> Lines { get; set; } = [];

    public bool CanAnnul { get; set; }
}

public sealed class ReceptionLineRequest : IValidatableObject
{
    [Required(ErrorMessage = "Indique la línea de la orden.")]
    [Range(1, int.MaxValue, ErrorMessage = "Indique la línea de la orden.")]
    public int? PurchaseOrderLineId { get; set; }

    [Range(0, 100000, ErrorMessage = "La cantidad aceptada debe ser un número entero entre 0 y 100,000.")]
    public int Accepted { get; set; }

    [Range(0, 100000, ErrorMessage = "La cantidad rechazada debe ser un número entero entre 0 y 100,000.")]
    public int Rejected { get; set; }

    [StringLength(500, ErrorMessage = "El motivo de rechazo no puede superar los 500 caracteres.")]
    [PlainText]
    public string? RejectReason { get; set; }

    /// <summary>Costo facturado si difiere del de la orden (sin IVA).</summary>
    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "El costo unitario debe estar entre 0.01 y 999,999.99.")]
    [MaxDecimals(2, "El costo unitario admite como máximo 2 decimales.")]
    public decimal? UnitCost { get; set; }

    [StringLength(500, ErrorMessage = "La nota de la línea no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Accepted + Rejected == 0)
        {
            yield return new ValidationResult("Indique cuántas unidades se aceptan o rechazan.", [nameof(Accepted)]);
        }

        if (Rejected > 0 && (RejectReason?.Trim().Length ?? 0) < 5)
        {
            yield return new ValidationResult(
                "Indique el motivo del rechazo (mínimo 5 caracteres).",
                [nameof(RejectReason)]);
        }
    }
}

/// <summary>Datos de la factura del proveedor y del ingreso.</summary>
public abstract class ReceptionHeaderRequest
{
    /// <summary>Si no se indica, el almacén principal.</summary>
    public int? WarehouseId { get; set; }

    /// <summary>Si no se indica, hoy.</summary>
    public DateOnly? ReceivedOn { get; set; }

    [StringLength(30, ErrorMessage = "La serie no puede superar los 30 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "La serie solo admite letras sin tildes, números y guion.")]
    public string? SupplierDocumentSeries { get; set; }

    [StringLength(20, ErrorMessage = "El número de factura no puede superar los 20 dígitos.")]
    [RegularExpression("^[0-9]+$", ErrorMessage = "El número de factura solo admite dígitos.")]
    [RequiredWhen(nameof(SupplierDocumentSeries), "Indique el número de la factura de esa serie.")]
    public string? SupplierDocumentNumber { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    protected IEnumerable<ValidationResult> ValidateHeader()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        if (ReceivedOn.HasValue && (ReceivedOn.Value > today || ReceivedOn.Value < today.AddDays(-30)))
        {
            yield return new ValidationResult(
                "La fecha de recepción no puede ser futura ni de hace más de 30 días.",
                [nameof(ReceivedOn)]);
        }
    }
}

public sealed class RegisterReceptionRequest : ReceptionHeaderRequest, IValidatableObject
{
    [Required(ErrorMessage = "Indique la orden de compra.")]
    public int? PurchaseOrderId { get; set; }

    [UniqueBy(nameof(ReceptionLineRequest.PurchaseOrderLineId), "Cada línea de la orden solo puede aparecer una vez.")]
    public List<ReceptionLineRequest> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines.Count == 0)
        {
            yield return new ValidationResult("Indique al menos una línea recibida.", [nameof(Lines)]);
        }

        foreach (ValidationResult result in NestedValidation.Validate(Lines, nameof(Lines)))
        {
            yield return result;
        }

        foreach (ValidationResult result in ValidateHeader())
        {
            yield return result;
        }
    }
}

/// <summary>
/// Compra directa: crea la orden ya emitida y la recibe completa en un solo
/// paso, de modo que el inventario se actualiza al registrar la compra.
/// </summary>
public sealed class DirectPurchaseRequest : ReceptionHeaderRequest, IValidatableObject
{
    [Required(ErrorMessage = "Indique los datos de la compra.")]
    public SavePurchaseOrderRequest? Order { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => ValidateHeader();
}

public sealed class AnnulReceptionRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;
}

public sealed class ReceptionQuery
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public int? PurchaseOrderId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class ReceptionCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> Warehouses { get; set; } = [];
}
