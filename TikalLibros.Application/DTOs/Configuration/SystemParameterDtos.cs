using System.ComponentModel.DataAnnotations;

namespace TikalLibros.Application.DTOs.Configuration;

public sealed class SystemParameterResponse
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    /// <summary>DECIMAL o INTEGER.</summary>
    public string DataType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEditable { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public sealed class UpdateSystemParameterRequest
{
    [Required(ErrorMessage = "Indique el valor.")]
    [StringLength(10, ErrorMessage = "El valor no puede superar los 10 caracteres.")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "El valor solo admite números con hasta 2 decimales.")]
    public string Value { get; set; } = string.Empty;
}
