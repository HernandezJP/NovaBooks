using System.ComponentModel.DataAnnotations;

namespace TikalLibros.Application.DTOs.Configuration;

public sealed class SystemParameterResponse
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    /// <summary>DECIMAL, INTEGER o STRING.</summary>
    public string DataType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEditable { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public sealed class UpdateSystemParameterRequest
{
    [Required(ErrorMessage = "Indique el valor.")]
    [StringLength(20, ErrorMessage = "El valor no puede superar los 20 caracteres.")]
    [RegularExpression(@"^[0-9A-Za-z.\-]+$", ErrorMessage = "El valor solo admite números, letras sin tildes, punto y guion.")]
    public string Value { get; set; } = string.Empty;
}
