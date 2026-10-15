using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Employees;

public sealed class EmployeeListItemResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Dpi { get; set; }

    public string Position { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateOnly HireDate { get; set; }

    public bool IsActive { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }
}

public sealed class EmployeeResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? SecondLastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Dpi { get; set; }

    public string? Email { get; set; }

    public int? PhoneTypeId { get; set; }

    public string? Phone { get; set; }

    public int PositionId { get; set; }

    public string Position { get; set; } = string.Empty;

    public int BranchId { get; set; }

    public string Branch { get; set; } = string.Empty;

    public DateOnly HireDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public bool? UserIsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public sealed class SaveEmployeeRequest : IValidatableObject
{
    public const int MinimumAge = 18;

    [Required(ErrorMessage = "El primer nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El primer nombre no puede superar los 100 caracteres.")]
    [PersonName("primer nombre")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo nombre no puede superar los 100 caracteres.")]
    [PersonName("segundo nombre")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El primer apellido no puede superar los 100 caracteres.")]
    [PersonName("primer apellido")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "El segundo apellido no puede superar los 100 caracteres.")]
    [PersonName("segundo apellido")]
    public string? SecondLastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "El DPI es obligatorio.")]
    [StringLength(15, ErrorMessage = "El DPI debe tener 13 dígitos.")]
    [Dpi]
    public string Dpi { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    public string? Email { get; set; }

    [RequiredWhen(nameof(Phone), "Seleccione el tipo de teléfono.")]
    public int? PhoneTypeId { get; set; }

    [StringLength(25, ErrorMessage = "El teléfono no puede superar los 25 caracteres.")]
    [PhoneNumber]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "El puesto es obligatorio.")]
    public int? PositionId { get; set; }

    [Required(ErrorMessage = "La sucursal es obligatoria.")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "La fecha de contratación es obligatoria.")]
    public DateOnly? HireDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        if (BirthDate.HasValue)
        {
            if (BirthDate.Value > today.AddYears(-MinimumAge))
            {
                yield return new ValidationResult(
                    $"El empleado debe tener al menos {MinimumAge} años.",
                    [nameof(BirthDate)]);
            }
            else if (BirthDate.Value < today.AddYears(-100))
            {
                yield return new ValidationResult(
                    "La fecha de nacimiento no es válida.",
                    [nameof(BirthDate)]);
            }
        }

        if (HireDate.HasValue)
        {
            if (HireDate.Value > today.AddDays(30))
            {
                yield return new ValidationResult(
                    "La fecha de contratación no puede ser mayor a 30 días en el futuro.",
                    [nameof(HireDate)]);
            }

            if (BirthDate.HasValue && HireDate.Value < BirthDate.Value.AddYears(MinimumAge))
            {
                yield return new ValidationResult(
                    $"La contratación debe ser posterior a que el empleado cumpliera {MinimumAge} años.",
                    [nameof(HireDate)]);
            }

            if (EndDate.HasValue && EndDate.Value < HireDate.Value)
            {
                yield return new ValidationResult(
                    "La fecha de finalización no puede ser anterior a la contratación.",
                    [nameof(EndDate)]);
            }
        }
    }
}

public sealed class ChangeEmployeeStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class EmployeeCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> Positions { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> Branches { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> PhoneTypes { get; set; } = [];
}
