using System.ComponentModel.DataAnnotations;
using NovaBooks.Application.Validation;

namespace NovaBooks.Application.DTOs.Editorials;

public sealed class EditorialResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Website { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int BooksCount { get; set; }

    public bool IsActive { get; set; }
}

public sealed class SaveEditorialRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Url(ErrorMessage = "El sitio web debe ser una dirección válida (https://...).")]
    [StringLength(500, ErrorMessage = "El sitio web no puede superar los 500 caracteres.")]
    public string? Website { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    public string? Email { get; set; }

    [StringLength(25, ErrorMessage = "El teléfono no puede superar los 25 caracteres.")]
    [PhoneNumber]
    public string? Phone { get; set; }
}

public sealed class ChangeEditorialStatusRequest
{
    public bool IsActive { get; set; }
}
