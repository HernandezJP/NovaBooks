using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Customers;

/// <summary>
/// Datos para crear o modificar un cliente. El código lo asigna el sistema.
/// </summary>
public sealed class SaveCustomerRequest : IValidatableObject
{
    public const string NaturalPerson = "NATURAL";

    public const string LegalPerson = "JURIDICA";

    [Required(ErrorMessage = "El tipo de persona es obligatorio.")]
    public string PersonType { get; set; } = NaturalPerson;

    [StringLength(100, ErrorMessage = "El primer nombre no puede superar los 100 caracteres.")]
    [RequiredWhen(nameof(PersonType), "El primer nombre es obligatorio.", Value = NaturalPerson)]
    [PersonName("primer nombre")]
    public string? FirstName { get; set; }

    [StringLength(100, ErrorMessage = "El segundo nombre no puede superar los 100 caracteres.")]
    [PersonName("segundo nombre")]
    public string? MiddleName { get; set; }

    [StringLength(100, ErrorMessage = "El primer apellido no puede superar los 100 caracteres.")]
    [RequiredWhen(nameof(PersonType), "El primer apellido es obligatorio.", Value = NaturalPerson)]
    [PersonName("primer apellido")]
    public string? LastName { get; set; }

    [StringLength(100, ErrorMessage = "El segundo apellido no puede superar los 100 caracteres.")]
    [PersonName("segundo apellido")]
    public string? SecondLastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    [StringLength(250, ErrorMessage = "La razón social no puede superar los 250 caracteres.")]
    [RequiredWhen(nameof(PersonType), "La razón social es obligatoria.", Value = LegalPerson)]
    [BusinessName("razón social")]
    public string? LegalName { get; set; }

    [StringLength(250, ErrorMessage = "El nombre comercial no puede superar los 250 caracteres.")]
    [BusinessName("nombre comercial", ErrorMessage = "El nombre comercial solo admite letras, números, espacios y los signos . , & ' - ( ) /.")]
    public string? TradeName { get; set; }

    /// <summary>
    /// Documento de identificación (DPI, pasaporte). El NIT va aparte.
    /// </summary>
    public int? IdentificationTypeId { get; set; }

    [StringLength(50, ErrorMessage = "La identificación no puede superar los 50 caracteres.")]
    [RegularExpression("^[A-Za-z0-9 -]+$", ErrorMessage = "La identificación solo admite letras sin tilde, números, espacios y guion.")]
    public string? IdentificationNumber { get; set; }

    [StringLength(20, ErrorMessage = "El NIT no puede superar los 20 caracteres.")]
    [Nit]
    [RequiredWhen(nameof(PersonType), "El NIT es obligatorio para personas jurídicas.", Value = LegalPerson)]
    public string? Nit { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    public string? Email { get; set; }

    [RequiredWhen(nameof(Phone), "Seleccione el tipo de teléfono.")]
    public int? PhoneTypeId { get; set; }

    [StringLength(25, ErrorMessage = "El teléfono no puede superar los 25 caracteres.")]
    [PhoneNumber]
    public string? Phone { get; set; }

    [StringLength(10, ErrorMessage = "La extensión no puede superar los 10 caracteres.")]
    [DigitsOnly(ErrorMessage = "La extensión solo admite dígitos.")]
    public string? PhoneExtension { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999",
        ErrorMessage = "El límite de crédito no puede ser negativo.")]
    public decimal CreditLimit { get; set; }

    [Range(0, 365, ErrorMessage = "Los días de crédito deben estar entre 0 y 365.")]
    public int CreditDays { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        string personType = PersonType?.Trim().ToUpperInvariant() ?? string.Empty;

        if (personType == NaturalPerson)
        {
            if (BirthDate.HasValue &&
                BirthDate.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult(
                    "La fecha de nacimiento no puede ser futura.",
                    [nameof(BirthDate)]);
            }
        }
        else if (personType != LegalPerson)
        {
            yield return new ValidationResult(
                "El tipo de persona debe ser NATURAL o JURIDICA.",
                [nameof(PersonType)]);
        }

        bool hasIdentificationType = IdentificationTypeId.HasValue;
        bool hasIdentificationNumber = !string.IsNullOrWhiteSpace(IdentificationNumber);

        if (hasIdentificationType != hasIdentificationNumber)
        {
            yield return new ValidationResult(
                "Indique el tipo y el número de identificación.",
                [nameof(IdentificationNumber)]);
        }
    }
}
