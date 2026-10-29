using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Suppliers;

public sealed class SupplierListItemResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string PersonType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? TradeName { get; set; }

    public string? Nit { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public decimal CreditLimit { get; set; }

    public int CreditDays { get; set; }

    public int OpenOrders { get; set; }

    public bool IsActive { get; set; }
}

public sealed class SupplierResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string PersonType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? SecondLastName { get; set; }

    public string? LegalName { get; set; }

    public string? TradeName { get; set; }

    public string? Nit { get; set; }

    public string? Email { get; set; }

    public int? PhoneTypeId { get; set; }

    public string? Phone { get; set; }

    public string? PhoneExtension { get; set; }

    public decimal CreditLimit { get; set; }

    public int CreditDays { get; set; }

    public string? Notes { get; set; }

    /// <summary>Órdenes en borrador, emitidas o recibidas parcialmente.</summary>
    public int OpenOrders { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public sealed class SaveSupplierRequest : IValidatableObject
{
    public const string NaturalPerson = "NATURAL";
    public const string LegalPerson = "JURIDICA";

    [Required(ErrorMessage = "El tipo de persona es obligatorio.")]
    public string PersonType { get; set; } = LegalPerson;

    [StringLength(100, ErrorMessage = "El primer nombre no puede superar los 100 caracteres.")]
    [PersonName("primer nombre")]
    [RequiredWhen(nameof(PersonType), "El primer nombre es obligatorio.", Value = NaturalPerson)]
    public string? FirstName { get; set; }

    [StringLength(100, ErrorMessage = "El segundo nombre no puede superar los 100 caracteres.")]
    [PersonName("segundo nombre")]
    public string? MiddleName { get; set; }

    [StringLength(100, ErrorMessage = "El primer apellido no puede superar los 100 caracteres.")]
    [PersonName("primer apellido")]
    [RequiredWhen(nameof(PersonType), "El primer apellido es obligatorio.", Value = NaturalPerson)]
    public string? LastName { get; set; }

    [StringLength(100, ErrorMessage = "El segundo apellido no puede superar los 100 caracteres.")]
    [PersonName("segundo apellido")]
    public string? SecondLastName { get; set; }

    [StringLength(250, ErrorMessage = "La razón social no puede superar los 250 caracteres.")]
    [BusinessName("razón social")]
    [RequiredWhen(nameof(PersonType), "La razón social es obligatoria.", Value = LegalPerson)]
    public string? LegalName { get; set; }

    [StringLength(250, ErrorMessage = "El nombre comercial no puede superar los 250 caracteres.")]
    [BusinessName("nombre comercial")]
    public string? TradeName { get; set; }

    [Required(ErrorMessage = "El NIT es obligatorio.")]
    [StringLength(20, ErrorMessage = "El NIT no puede superar los 20 caracteres.")]
    [Nit]
    public string Nit { get; set; } = string.Empty;

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

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "El límite de crédito no puede ser negativo.")]
    public decimal CreditLimit { get; set; }

    [Range(0, 365, ErrorMessage = "Los días de crédito deben estar entre 0 y 365.")]
    public int CreditDays { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        string personType = PersonType?.Trim().ToUpperInvariant() ?? string.Empty;

        if (personType is not (NaturalPerson or LegalPerson))
        {

            yield return new ValidationResult("El tipo de persona debe ser NATURAL o JURIDICA.", [nameof(PersonType)]);
        }

        if (CreditDays > 0 && CreditLimit == 0)
        {
            yield return new ValidationResult(
                "Indique el límite de crédito o deje los días de crédito en 0.",
                [nameof(CreditLimit)]);
        }
    }
}

public sealed class ChangeSupplierStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class SupplierCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> PersonTypes { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> PhoneTypes { get; set; } = [];
}
