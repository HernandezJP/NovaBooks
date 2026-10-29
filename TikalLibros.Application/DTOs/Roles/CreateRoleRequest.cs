using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Roles;

public sealed class CreateRoleRequest
{
    [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 3,
        ErrorMessage =
            "El nombre del rol debe contener entre 3 y 100 caracteres.")]
    [RegularExpression(@"^[\p{L}\p{M}0-9][\p{L}\p{M}0-9 ._-]*$", ErrorMessage = "El nombre del rol solo admite letras, números, espacios, punto, guion y guion bajo.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        500,
        ErrorMessage =
            "La descripción no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Description { get; set; }
}
