using System.ComponentModel.DataAnnotations;
using NovaBooks.Application.Validation;

namespace NovaBooks.Application.DTOs.Customers;

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
    public string? FirstName { get; set; }

    [StringLength(100, ErrorMessage = "El segundo nombre no puede superar los 100 caracteres.")]
    public string? MiddleName { get; set; }

    [StringLength(100, ErrorMessage = "El primer apellido no puede superar los 100 caracteres.")]
    public string? LastName { get; set; }

    [StringLength(100, ErrorMessage = "El segundo apellido no puede superar los 100 caracteres.")]
    public string? SecondLastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    [StringLength(250, ErrorMessage = "La razón social no puede superar los 250 caracteres.")]
    public string? LegalName { get; set; }

    [StringLength(250, ErrorMessage = "El nombre comercial no puede superar los 250 caracteres.")]
    public string? TradeName { get; set; }

    /// <summary>
    /// Documento de identificación (DPI, pasaporte). El NIT va aparte.
    /// </summary>
    public int? IdentificationTypeId { get; set; }

    [StringLength(50, ErrorMessage = "La identificación no puede superar los 50 caracteres.")]
    public string? IdentificationNumber { get; set; }

    [StringLength(20, ErrorMessage = "El NIT no puede superar los 20 caracteres.")]
    [Nit]
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
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        string personType = PersonType?.Trim().ToUpperInvariant() ?? string.Empty;

        if (personType == NaturalPerson)
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                yield return new ValidationResult(
                    "El primer nombre es obligatorio.",
                    [nameof(FirstName)]);
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                yield return new ValidationResult(
                    "El primer apellido es obligatorio.",
                    [nameof(LastName)]);
            }

            if (BirthDate.HasValue &&
                BirthDate.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult(
                    "La fecha de nacimiento no puede ser futura.",
                    [nameof(BirthDate)]);
            }
        }
        else if (personType == LegalPerson)
        {
            if (string.IsNullOrWhiteSpace(LegalName))
            {
                yield return new ValidationResult(
                    "La razón social es obligatoria.",
                    [nameof(LegalName)]);
            }

            if (string.IsNullOrWhiteSpace(Nit))
            {
                yield return new ValidationResult(
                    "El NIT es obligatorio para personas jurídicas.",
                    [nameof(Nit)]);
            }
        }
        else
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
