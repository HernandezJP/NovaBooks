using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Purchasing;

public sealed class SupplierReturnQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public int? SupplierId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public bool IncludeAnnulled { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class SupplierReturnListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int ReceptionId { get; set; }

    public string ReceptionNumber { get; set; } = string.Empty;

    public string Supplier { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public decimal Units { get; set; }

    /// <summary>Valor al costo de compra: lo que el proveedor debe acreditar.</summary>
    public decimal Total { get; set; }

    public string? Employee { get; set; }

    public bool Annulled { get; set; }
}

public sealed class SupplierReturnLineResponse
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? LotNumber { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Total { get; set; }
}

public sealed class SupplierReturnResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int ReceptionId { get; set; }

    public string ReceptionNumber { get; set; } = string.Empty;

    public string PurchaseOrderNumber { get; set; } = string.Empty;

    public string Supplier { get; set; } = string.Empty;

    public string? SupplierDocument { get; set; }

    public string Warehouse { get; set; } = string.Empty;

    public string? Employee { get; set; }

    public string Reason { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public bool Annulled { get; set; }

    public DateTime? AnnulledAt { get; set; }

    public string? AnnulmentReason { get; set; }

    public IReadOnlyList<SupplierReturnLineResponse> Lines { get; set; } = [];
}

/// <summary>Línea de una recepción con lo que aún puede devolverse al proveedor.</summary>
public sealed class ReturnableLineResponse
{
    public int ReceptionLineId { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? LotNumber { get; set; }

    public decimal Accepted { get; set; }

    /// <summary>Unidades del lote que siguen en el almacén y libres de reservas.</summary>
    public decimal Returnable { get; set; }

    public decimal UnitCost { get; set; }
}

public sealed class ReturnableReceptionResponse
{
    public int ReceptionId { get; set; }

    public string ReceptionNumber { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public string Supplier { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public IReadOnlyList<ReturnableLineResponse> Lines { get; set; } = [];
}

public sealed class SupplierReturnLineRequest
{
    [Required(ErrorMessage = "Seleccione la línea de la recepción.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la línea de la recepción.")]
    public int? ReceptionLineId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe ser un número entero entre 1 y 100,000.")]
    public int? Quantity { get; set; }
}

public sealed class SaveSupplierReturnRequest : IValidatableObject
{
    public const int MaxLines = 200;

    [Required(ErrorMessage = "Seleccione la recepción.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la recepción.")]
    public int? ReceptionId { get; set; }

    [Required(ErrorMessage = "Indique el motivo de la devolución.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;

    [UniqueBy(nameof(SupplierReturnLineRequest.ReceptionLineId), "Cada línea de la recepción solo puede aparecer una vez.")]
    public List<SupplierReturnLineRequest> Lines { get; set; } = [];

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

public sealed class AnnulSupplierReturnRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;
}
