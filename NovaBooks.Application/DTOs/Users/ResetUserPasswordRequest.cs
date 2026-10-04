using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Application.DTOs.Users;

public sealed class ResetUserPasswordRequest
{
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage =
            "La contraseña debe contener entre 8 y 100 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}