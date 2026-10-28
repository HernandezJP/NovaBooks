using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Inventory;

public sealed class TransferQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    /// <summary>Traslados que salen de este almacén o llegan a él.</summary>
    public int? WarehouseId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public bool IncludeAnnulled { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class TransferListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string SourceWarehouse { get; set; } = string.Empty;

    public string SourceBranch { get; set; } = string.Empty;

    public string TargetWarehouse { get; set; } = string.Empty;

    public string TargetBranch { get; set; } = string.Empty;

    public string? Employee { get; set; }

    public int Books { get; set; }

    public decimal Units { get; set; }

    public string? Notes { get; set; }

    public bool Annulled { get; set; }
}

public sealed class TransferLineRequest
{
    [Required(ErrorMessage = "Seleccione el libro.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el libro.")]
    public int? BookId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe ser un número entero entre 1 y 100,000.")]
    public int? Quantity { get; set; }
}

public sealed class SaveTransferRequest : IValidatableObject
{
    public const int MaxLines = 200;

    [Required(ErrorMessage = "Seleccione el almacén de origen.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén de origen.")]
    public int? SourceWarehouseId { get; set; }

    [Required(ErrorMessage = "Seleccione el almacén de destino.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén de destino.")]
    public int? TargetWarehouseId { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    [UniqueBy(nameof(TransferLineRequest.BookId), "Cada libro solo puede aparecer una vez en el traslado.")]
    public List<TransferLineRequest> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SourceWarehouseId.HasValue && SourceWarehouseId == TargetWarehouseId)
        {
            yield return new ValidationResult("El almacén de destino debe ser distinto del de origen.", [nameof(TargetWarehouseId)]);
        }

        if (Lines.Count == 0)
        {
            yield return new ValidationResult("Agregue al menos un libro al traslado.", [nameof(Lines)]);
        }
        else if (Lines.Count > MaxLines)
        {
            yield return new ValidationResult($"El traslado admite como máximo {MaxLines} libros.", [nameof(Lines)]);
        }

        foreach (ValidationResult result in NestedValidation.Validate(Lines, nameof(Lines)))
        {
            yield return result;
        }
    }
}

public sealed class AnnulTransferRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;
}
