using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Application.DTOs.Users;

public sealed class UpdateUserRequest
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(
        100,
        MinimumLength = 3,
        ErrorMessage =
            "El nombre de usuario debe contener entre 3 y 100 caracteres.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(
        256,
        ErrorMessage =
            "El correo electrónico no puede superar los 256 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El número de teléfono no es válido.")]
    [StringLength(
        30,
        ErrorMessage =
            "El teléfono no puede superar los 30 caracteres.")]
    public string? PhoneNumber { get; set; }
}