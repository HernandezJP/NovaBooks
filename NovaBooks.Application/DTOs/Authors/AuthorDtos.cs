using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Application.DTOs.Authors;

public sealed class AuthorResponse
{
    public int Id { get; set; }

    /// <summary>Seudónimo o, si no tiene, nombre y apellido.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? SecondLastName { get; set; }

    public string? Pseudonym { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? DeathDate { get; set; }

    public string? Biography { get; set; }

    public int BooksCount { get; set; }

    public bool IsActive { get; set; }
}

public sealed class SaveAuthorRequest : IValidatableObject
{
    [Required(ErrorMessage = "El primer nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El primer nombre no puede superar los 100 caracteres.")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo nombre no puede superar los 100 caracteres.")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El primer apellido no puede superar los 100 caracteres.")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo apellido no puede superar los 100 caracteres.")]
    public string? SecondLastName { get; set; }

    [StringLength(150, ErrorMessage = "El seudónimo no puede superar los 150 caracteres.")]
    public string? Pseudonym { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? DeathDate { get; set; }

    [StringLength(4000, ErrorMessage = "La biografía no puede superar los 4000 caracteres.")]
    public string? Biography { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        if (BirthDate > today)
        {
            yield return new ValidationResult(
                "La fecha de nacimiento no puede ser futura.",
                [nameof(BirthDate)]);
        }

        if (DeathDate > today)
        {
            yield return new ValidationResult(
                "La fecha de fallecimiento no puede ser futura.",
                [nameof(DeathDate)]);
        }

        if (BirthDate.HasValue && DeathDate.HasValue && DeathDate < BirthDate)
        {
            yield return new ValidationResult(
                "La fecha de fallecimiento no puede ser anterior a la de nacimiento.",
                [nameof(DeathDate)]);
        }
    }
}

public sealed class ChangeAuthorStatusRequest
{
    public bool IsActive { get; set; }
}
