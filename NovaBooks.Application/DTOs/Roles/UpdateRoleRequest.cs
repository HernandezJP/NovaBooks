using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Application.DTOs.Roles;

public sealed class UpdateRoleRequest
{
    [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 3,
        ErrorMessage =
            "El nombre del rol debe contener entre 3 y 100 caracteres.")]
    public string Name { get; set; } = string.Empty;
}