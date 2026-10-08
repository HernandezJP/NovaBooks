using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NovaBooks.Application.Validation;

// Reglas de un solo campo como atributos: se evalúan junto con [Required],
// [StringLength], etc., así el usuario ve todos los errores a la vez.
// IValidatableObject.Validate queda para reglas que combinan campos.

/// <summary>Teléfono de 8 a 15 dígitos (admite espacios, guiones y +).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PhoneNumberAttribute : ValidationAttribute
{
    public PhoneNumberAttribute()
        : base("El teléfono debe contener entre 8 y 15 dígitos.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        string.IsNullOrWhiteSpace(text) ||
        ContactRules.IsValidPhone(ContactRules.NormalizePhone(text));
}

/// <summary>NIT: dígitos con verificador final numérico o K.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NitAttribute : ValidationAttribute
{
    public NitAttribute()
        : base("El NIT debe contener solo dígitos y un dígito verificador (0-9 o K).")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        string.IsNullOrWhiteSpace(text) ||
        ContactRules.IsValidNitFormat(ContactRules.NormalizeIdentification(text));
}

/// <summary>Solo dígitos (por ejemplo, extensión telefónica).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DigitsOnlyAttribute : ValidationAttribute
{
    public DigitsOnlyAttribute()
        : base("Solo se admiten dígitos.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        string.IsNullOrWhiteSpace(text) ||
        ContactRules.IsDigitsOnly(text.Trim());
}

public enum IsbnKind
{
    Isbn10,
    Isbn13
}

/// <summary>ISBN-10 o ISBN-13 con dígito de control válido.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IsbnAttribute : ValidationAttribute
{
    private readonly IsbnKind _kind;

    public IsbnAttribute(IsbnKind kind)
        : base(kind == IsbnKind.Isbn10
            ? "El ISBN-10 no es válido: revise los dígitos y el dígito de control."
            : "El ISBN-13 no es válido: debe iniciar con 978 o 979 y tener un dígito de control correcto.")
    {
        _kind = kind;
    }

    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        string normalized = IsbnRules.Normalize(text);

        return _kind == IsbnKind.Isbn10
            ? IsbnRules.IsValidIsbn10(normalized)
            : IsbnRules.IsValidIsbn13(normalized);
    }
}

/// <summary>
/// En una lista de objetos, la propiedad indicada no se repite
/// (por ejemplo, cada lista de precios una sola vez).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueByAttribute : ValidationAttribute
{
    private readonly string _propertyName;

    public UniqueByAttribute(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        _propertyName = propertyName;
    }

    public override bool IsValid(object? value)
    {
        if (value is not IEnumerable items)
        {
            return true;
        }

        HashSet<object?> seen = [];

        foreach (object? item in items)
        {
            if (item is null)
            {
                continue;
            }

            PropertyInfo? property = item.GetType().GetProperty(_propertyName);

            if (property is not null && !seen.Add(property.GetValue(item)))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Obligatorio cuando otra propiedad tiene valor (por ejemplo, el tipo de
/// teléfono si se escribió un teléfono).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredWhenAttribute : ValidationAttribute
{
    private readonly string _otherProperty;

    public RequiredWhenAttribute(string otherProperty, string errorMessage)
        : base(errorMessage)
    {
        _otherProperty = otherProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        object? other = validationContext.ObjectType
            .GetProperty(_otherProperty)?
            .GetValue(validationContext.ObjectInstance);

        bool otherHasValue = other is string text ? !string.IsNullOrWhiteSpace(text) : other is not null;
        bool hasValue = value is string own ? !string.IsNullOrWhiteSpace(own) : value is not null;

        return otherHasValue && !hasValue
            ? new ValidationResult(ErrorMessage, validationContext.MemberName is { } member ? [member] : null)
            : ValidationResult.Success;
    }
}
