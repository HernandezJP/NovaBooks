using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.PriceLists;

public sealed class PriceListResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Libros con un precio vigente en la lista.</summary>
    public int BooksCount { get; set; }
}

public sealed class SavePriceListRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [BusinessName("nombre", ErrorMessage = "El nombre solo admite letras, números, espacios y los signos . , & ' - ( ) /.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Description { get; set; }

    /// <summary>Lista que se propone en ventas y pedidos (solo una).</summary>
    public bool IsDefault { get; set; }
}
