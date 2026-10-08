using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NovaBooks.Application.Validation;

namespace NovaBooks.Application.DTOs.Products;

/// <summary>
/// Datos para crear o modificar un libro.
/// </summary>
public sealed class SaveProductRequest : IValidatableObject
{
    [StringLength(13, ErrorMessage = "El ISBN-10 no es válido.")]
    [Isbn(IsbnKind.Isbn10)]
    public string? Isbn10 { get; set; }

    [StringLength(17, ErrorMessage = "El ISBN-13 no es válido.")]
    [Isbn(IsbnKind.Isbn13)]
    public string? Isbn13 { get; set; }

    [StringLength(50, ErrorMessage = "El código de barras no puede superar los 50 caracteres.")]
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(250, ErrorMessage = "El título no puede superar los 250 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "El subtítulo no puede superar los 250 caracteres.")]
    public string? Subtitle { get; set; }

    public int? EditorialId { get; set; }

    [Required(ErrorMessage = "El idioma es obligatorio.")]
    public int? LanguageId { get; set; }

    [Required(ErrorMessage = "El formato es obligatorio.")]
    public int? FormatId { get; set; }

    [Required(ErrorMessage = "El impuesto es obligatorio.")]
    public int? TaxId { get; set; }

    [StringLength(50, ErrorMessage = "La edición no puede superar los 50 caracteres.")]
    public string? Edition { get; set; }

    [Range(1000, 9999, ErrorMessage = "El año de publicación no es válido.")]
    public int? PublicationYear { get; set; }

    [Range(1, 100000, ErrorMessage = "El número de páginas debe ser mayor que cero.")]
    public int? PageCount { get; set; }

    [StringLength(4000, ErrorMessage = "La descripción no puede superar los 4000 caracteres.")]
    public string? Description { get; set; }

    public bool AllowsSale { get; set; } = true;

    /// <summary>
    /// Autores en el orden en que deben mostrarse.
    /// </summary>
    public List<int> AuthorIds { get; set; } = [];

    public List<int> CategoryIds { get; set; } = [];

    /// <summary>
    /// Debe estar en CategoryIds; si se omite se usa la primera.
    /// </summary>
    public int? PrimaryCategoryId { get; set; }

    /// <summary>
    /// Precios por lista. Null deja los precios sin cambios.
    /// Requiere Catalogo.AdministrarPrecios.
    /// </summary>
    [UniqueBy(nameof(ProductPriceRequest.PriceListId), "Cada lista de precios solo puede enviarse una vez.")]
    public List<ProductPriceRequest>? Prices { get; set; }

    /// <summary>
    /// Null deja el costo sin cambios. Requiere Catalogo.AdministrarPrecios.
    /// </summary>
    [Range(typeof(decimal), "0", "9999999999999999",
        ErrorMessage = "El costo de referencia no puede ser negativo.")]
    public decimal? ReferenceCost { get; set; }

    [JsonIgnore]
    public bool ChangesPricing =>
        Prices is not null || ReferenceCost.HasValue;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        string? isbn10 = string.IsNullOrWhiteSpace(Isbn10)
            ? null
            : IsbnRules.Normalize(Isbn10);

        string? isbn13 = string.IsNullOrWhiteSpace(Isbn13)
            ? null
            : IsbnRules.Normalize(Isbn13);

        if (isbn10 is not null && isbn13 is not null &&
            IsbnRules.IsValidIsbn10(isbn10) &&
            IsbnRules.IsValidIsbn13(isbn13) &&
            isbn13.StartsWith("978", StringComparison.Ordinal) &&
            IsbnRules.ToIsbn13(isbn10) != isbn13)
        {
            yield return new ValidationResult(
                "El ISBN-10 y el ISBN-13 no corresponden al mismo libro.",
                [nameof(Isbn13)]);
        }

        if (PublicationYear > DateTime.Today.Year + 1)
        {
            yield return new ValidationResult(
                "El año de publicación no puede ser futuro.",
                [nameof(PublicationYear)]);
        }

        if (PrimaryCategoryId.HasValue &&
            !CategoryIds.Contains(PrimaryCategoryId.Value))
        {
            yield return new ValidationResult(
                "La categoría principal debe estar entre las categorías seleccionadas.",
                [nameof(PrimaryCategoryId)]);
        }

    }
}

public sealed class ProductPriceRequest
{
    [Required(ErrorMessage = "La lista de precios es obligatoria.")]
    public int PriceListId { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999",
        ErrorMessage = "El precio no puede ser negativo.")]
    public decimal Price { get; set; }
}
