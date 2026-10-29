using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Categories;

public sealed class CategoryResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ParentId { get; set; }

    public string? Parent { get; set; }

    public int BooksCount { get; set; }

    public int SubcategoriesCount { get; set; }

    public bool IsActive { get; set; }
}

public sealed class SaveCategoryRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [BusinessName("nombre", ErrorMessage = "El nombre solo admite letras, números, espacios y los signos . , & ' - ( ) /.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Description { get; set; }

    /// <summary>Categoría padre; null para una categoría principal.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "La categoría padre no es válida.")]
    public int? ParentId { get; set; }
}
