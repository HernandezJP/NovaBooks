using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Inventory;

/// <summary>Estado de una existencia frente a sus niveles mínimo y máximo.</summary>
public static class StockStatus
{
    public const string OutOfStock = "AGOTADO";
    public const string BelowMinimum = "BAJO_MINIMO";
    public const string AboveMaximum = "SOBRE_MAXIMO";
    public const string Normal = "NORMAL";

    public static string Label(string status) => status switch
    {
        OutOfStock => "Agotado",
        BelowMinimum => "Bajo el mínimo",
        AboveMaximum => "Sobre el máximo",
        _ => "Normal"
    };
}

public sealed class StockQuery
{
    public string? Search { get; set; }

    public int? WarehouseId { get; set; }

    /// <summary>AGOTADO, BAJO_MINIMO, SOBRE_MAXIMO o NORMAL.</summary>
    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class StockItemResponse
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Isbn { get; set; }

    public int WarehouseId { get; set; }

    public string Warehouse { get; set; } = string.Empty;

    public decimal Available { get; set; }

    public decimal Reserved { get; set; }

    public decimal Free { get; set; }

    public decimal Minimum { get; set; }

    public decimal Maximum { get; set; }

    public string? Location { get; set; }

    public DateTime? LastMovementAt { get; set; }

    public string Status { get; set; } = StockStatus.Normal;

    public string StatusLabel { get; set; } = string.Empty;

    /// <summary>Costo promedio de los lotes vigentes (solo con Inventario.VerCostos).</summary>
    public decimal? AverageCost { get; set; }

    public decimal? TotalValue { get; set; }
}

public sealed class SaveStockLevelsRequest : IValidatableObject
{
    [Required(ErrorMessage = "Seleccione el almacén.")]
    public int? WarehouseId { get; set; }

    [Required(ErrorMessage = "Seleccione el libro.")]
    public int? BookId { get; set; }

    [Range(0, 100000, ErrorMessage = "El mínimo debe ser un número entero entre 0 y 100,000.")]
    public int Minimum { get; set; }

    /// <summary>0 significa sin máximo.</summary>
    [Range(0, 100000, ErrorMessage = "El máximo debe ser un número entero entre 0 y 100,000.")]
    public int Maximum { get; set; }

    [StringLength(100, ErrorMessage = "La ubicación no puede superar los 100 caracteres.")]
    [RegularExpression(@"^[\p{L}0-9][\p{L}0-9 .\-/#]*$", ErrorMessage = "La ubicación solo admite letras, números, espacios y los signos . - / #.")]
    public string? Location { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Maximum > 0 && Maximum < Minimum)
        {
            yield return new ValidationResult(
                "El máximo debe ser mayor o igual al mínimo (o 0 para no usar máximo).",
                [nameof(Maximum)]);
        }
    }
}

public sealed class StockAlertResponse
{
    public int StockId { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public decimal Available { get; set; }

    public decimal Minimum { get; set; }

    public decimal Maximum { get; set; }

    public string Level { get; set; } = string.Empty;

    public string LevelLabel { get; set; } = string.Empty;

    /// <summary>Unidades para llegar al máximo (o al doble del mínimo si no hay máximo).</summary>
    public decimal SuggestedOrder { get; set; }
}

public sealed class StockAlertSummaryResponse
{
    public int OutOfStock { get; set; }

    public int BelowMinimum { get; set; }

    public int AboveMaximum { get; set; }

    public int Total => OutOfStock + BelowMinimum + AboveMaximum;
}

public sealed class MovementQuery
{
    public string? Search { get; set; }

    public string? Type { get; set; }

    public int? WarehouseId { get; set; }

    public int? BookId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public bool IncludeAnnulled { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class MovementListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    /// <summary>1 entrada, -1 salida, 0 transferencia.</summary>
    public int Nature { get; set; }

    public DateTime Date { get; set; }

    public string? Warehouse { get; set; }

    public string? DocumentReference { get; set; }

    public string? Employee { get; set; }

    public decimal Units { get; set; }

    public bool Annulled { get; set; }
}

public sealed class MovementLineResponse
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? LotNumber { get; set; }

    public decimal Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal? TotalCost { get; set; }
}

public sealed class MovementResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int Nature { get; set; }

    public DateTime Date { get; set; }

    public string? SourceWarehouse { get; set; }

    public string? TargetWarehouse { get; set; }

    public string? DocumentReference { get; set; }

    public string? Employee { get; set; }

    public string? Notes { get; set; }

    public bool Annulled { get; set; }

    public DateTime? AnnulledAt { get; set; }

    public string? AnnulmentReason { get; set; }

    public int? ReceptionId { get; set; }

    public IReadOnlyCollection<MovementLineResponse> Lines { get; set; } = [];
}

public sealed class KardexQuery
{
    [Required(ErrorMessage = "Seleccione el libro.")]
    public int? BookId { get; set; }

    public int? WarehouseId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }
}

public sealed class KardexEntryResponse
{
    public int MovementId { get; set; }

    public string MovementNumber { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public string? DocumentReference { get; set; }

    public string? LotNumber { get; set; }

    public decimal In { get; set; }

    public decimal Out { get; set; }

    public decimal Balance { get; set; }

    public decimal? UnitCost { get; set; }
}

public sealed class KardexResponse
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Warehouse { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal TotalIn { get; set; }

    public decimal TotalOut { get; set; }

    public decimal ClosingBalance { get; set; }

    /// <summary>Movimientos del período sin los anulados (máximo 1000).</summary>
    public IReadOnlyCollection<KardexEntryResponse> Entries { get; set; } = [];

    public bool Truncated { get; set; }
}

public sealed class AdjustmentLineRequest
{
    [Required(ErrorMessage = "Seleccione el libro.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el libro.")]
    public int? BookId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 100000, ErrorMessage = "La cantidad debe ser un número entero entre 1 y 100,000.")]
    public int? Quantity { get; set; }

    /// <summary>
    /// Solo para ajustes positivos: costo del lote nuevo. Si no se indica,
    /// el del último lote o el costo de referencia del libro.
    /// </summary>
    [Range(typeof(decimal), "0", "999999.99", ErrorMessage = "El costo unitario debe estar entre 0 y 999,999.99.")]
    [MaxDecimals(2, "El costo unitario admite como máximo 2 decimales.")]
    public decimal? UnitCost { get; set; }

    [StringLength(500, ErrorMessage = "La nota de la línea no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }
}

public sealed class SaveAdjustmentRequest : IValidatableObject
{
    public const int MaxLines = 200;

    /// <summary>Si no se indica, el almacén principal.</summary>
    public int? WarehouseId { get; set; }

    [Required(ErrorMessage = "Seleccione el motivo del ajuste.")]
    public int? ReasonId { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    [UniqueBy(nameof(AdjustmentLineRequest.BookId), "Cada libro solo puede aparecer una vez en el ajuste.")]
    public List<AdjustmentLineRequest> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines.Count == 0)
        {
            yield return new ValidationResult("Agregue al menos un libro al ajuste.", [nameof(Lines)]);
        }
        else if (Lines.Count > MaxLines)
        {
            yield return new ValidationResult($"El ajuste admite como máximo {MaxLines} libros.", [nameof(Lines)]);
        }

        foreach (ValidationResult result in NestedValidation.Validate(Lines, nameof(Lines)))
        {
            yield return result;
        }
    }
}

public sealed class AdjustmentLineResponse
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal SystemQuantity { get; set; }

    public decimal PhysicalQuantity { get; set; }

    public decimal Difference { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Notes { get; set; }
}

public sealed class AdjustmentResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int Nature { get; set; }

    public DateTime Date { get; set; }

    public string Employee { get; set; } = string.Empty;

    public string? MovementNumber { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyCollection<AdjustmentLineResponse> Lines { get; set; } = [];
}

public sealed class AdjustmentListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int Nature { get; set; }

    public DateTime Date { get; set; }

    public string Employee { get; set; } = string.Empty;

    public decimal Units { get; set; }
}

public sealed class AdjustmentReasonResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>1 suma existencias, -1 resta.</summary>
    public int Nature { get; set; }

    public bool RequiresNotes { get; set; }
}

public sealed class InventoryCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> Warehouses { get; set; } = [];

    public IReadOnlyCollection<AdjustmentReasonResponse> AdjustmentReasons { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> MovementTypes { get; set; } = [];
}
