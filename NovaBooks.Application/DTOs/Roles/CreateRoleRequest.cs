using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Application.DTOs.Roles;

public sealed class CreateRoleRequest
{
    [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 3,
        ErrorMessage =
            "El nombre del rol debe contener entre 3 y 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        500,
        ErrorMessage =
            "La descripción no puede superar los 500 caracteres.")]
    public string? Description { get; set; }
}
