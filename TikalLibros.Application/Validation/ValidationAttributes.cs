using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TikalLibros.Application.Validation;

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

/// <summary>Nombre o apellido de persona: solo letras, sin números.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PersonNameAttribute : ValidationAttribute
{
    public PersonNameAttribute(string fieldLabel)
        : base($"El {fieldLabel} solo puede contener letras.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        string.IsNullOrWhiteSpace(text) ||
        ContactRules.IsValidPersonName(text);
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

    /// <summary>
    /// Si se indica, el campo es obligatorio solo cuando la otra propiedad
    /// tiene exactamente este valor (sin distinguir mayúsculas), por ejemplo
    /// el tipo de persona "JURIDICA".
    /// </summary>
    public string? Value { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        object? other = validationContext.ObjectType
            .GetProperty(_otherProperty)?
            .GetValue(validationContext.ObjectInstance);

        bool otherHasValue = Value is not null
            ? string.Equals(other?.ToString()?.Trim(), Value, StringComparison.OrdinalIgnoreCase)
            : other is string text ? !string.IsNullOrWhiteSpace(text) : other is not null;

        bool hasValue = value is string own ? !string.IsNullOrWhiteSpace(own) : value is not null;

        return otherHasValue && !hasValue
            ? new ValidationResult(ErrorMessage, validationContext.MemberName is { } member ? [member] : null)
            : ValidationResult.Success;
    }
}

/// <summary>DPI (CUI) de Guatemala con dígito verificador válido.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DpiAttribute : ValidationAttribute
{
    public DpiAttribute()
        : base("El DPI debe tener 13 dígitos válidos (correlativo, verificador, departamento y municipio).")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        string.IsNullOrWhiteSpace(text) ||
        DpiRules.IsValid(DpiRules.Normalize(text));
}

/// <summary>
/// Nombre de usuario: letras sin tildes, dígitos, punto, guion y guion
/// bajo; empieza con letra o dígito. Sin espacios.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed partial class UserNameAttribute : ValidationAttribute
{
    public UserNameAttribute()
        : base("El nombre de usuario solo admite letras sin tildes, números, punto, guion y guion bajo, sin espacios.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        string.IsNullOrWhiteSpace(text) ||
        UserNameRegex().IsMatch(text.Trim());

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial System.Text.RegularExpressions.Regex UserNameRegex();
}

/// <summary>
/// Razón social o nombre comercial: letras, dígitos, espacios y los signos
/// habituales en nombres de empresa (. , &amp; ' - ( ) /). Empieza con letra o
/// dígito.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed partial class BusinessNameAttribute : ValidationAttribute
{
    public BusinessNameAttribute(string fieldLabel)
        : base($"La {fieldLabel} solo admite letras, números, espacios y los signos . , & ' - ( ) /.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        string.IsNullOrWhiteSpace(text) ||
        BusinessNameRegex().IsMatch(text.Trim());

    [System.Text.RegularExpressions.GeneratedRegex(@"^[\p{L}\p{N}][\p{L}\p{M}\p{N} .,&'â€™\-()/]*$")]
    private static partial System.Text.RegularExpressions.Regex BusinessNameRegex();
}

/// <summary>Número decimal con como máximo la cantidad de decimales indicada.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MaxDecimalsAttribute : ValidationAttribute
{
    private readonly int _decimals;

    public MaxDecimalsAttribute(int decimals, string errorMessage)
        : base(errorMessage)
    {
        _decimals = decimals;
    }

    public override bool IsValid(object? value) =>
        value is not decimal number ||
        decimal.Round(number, _decimals) == number;
}

/// <summary>
/// Texto libre (motivos, notas): admite letras, números y puntuación, pero
/// no los signos &lt; &gt; ni caracteres de control (salvo saltos de línea).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PlainTextAttribute : ValidationAttribute
{
    public PlainTextAttribute()
        : base("El texto no puede contener los signos < > ni caracteres especiales de control.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text ||
        text.All(character =>
            character is not ('<' or '>') &&
            (!char.IsControl(character) || character is '\r' or '\n' or '\t'));
}

/// <summary>
/// Valida los elementos de una lista y antepone "Línea N:" a cada mensaje;
/// las claves quedan como "Lines[0].Campo", igual que en la API.
/// </summary>
public static class NestedValidation
{
    public static IEnumerable<ValidationResult> Validate<T>(IReadOnlyList<T?> items, string collectionName)
        where T : class
    {
        for (int index = 0; index < items.Count; index++)
        {
            if (items[index] is not { } item)
            {
                continue;
            }

            List<ValidationResult> results = [];
            Validator.TryValidateObject(item, new ValidationContext(item), results, validateAllProperties: true);

            foreach (ValidationResult result in results)
            {
                yield return new ValidationResult(
                    $"Línea {index + 1}: {result.ErrorMessage}",
                    result.MemberNames.Select(member => $"{collectionName}[{index}].{member}").ToArray());
            }
        }
    }
}
