using System.Text.RegularExpressions;

namespace NovaBooks.Application.Validation;

/// <summary>
/// Normalización y validación de códigos, teléfonos e identificaciones.
/// </summary>
public static partial class ContactRules
{
    public static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    public static bool IsValidCode(string normalizedCode, int maxLength = 20)
    {
        return normalizedCode.Length <= maxLength &&
               CodeRegex().IsMatch(normalizedCode);
    }

    /// <summary>
    /// Quita espacios, guiones, puntos y paréntesis; conserva '+' inicial.
    /// </summary>
    public static string NormalizePhone(string value)
    {
        return PhoneSeparatorsRegex().Replace(value.Trim(), string.Empty);
    }

    public static bool IsValidPhone(string normalizedPhone)
    {
        return PhoneRegex().IsMatch(normalizedPhone);
    }

    /// <summary>
    /// Mayúsculas, sin espacios ni guiones (por ejemplo, NIT 1234567-K).
    /// </summary>
    public static string NormalizeIdentification(string value)
    {
        return IdentificationSeparatorsRegex()
            .Replace(value.Trim(), string.Empty)
            .ToUpperInvariant();
    }

    /// <summary>
    /// NIT guatemalteco: dígitos con dígito verificador final numérico o K.
    /// </summary>
    public static bool IsValidNitFormat(string normalizedNit)
    {
        return NitRegex().IsMatch(normalizedNit);
    }

    public static bool IsDigitsOnly(string value)
    {
        return value.Length > 0 && value.All(char.IsAsciiDigit);
    }

    public static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9_-]*$")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"[\s\-\.\(\)]")]
    private static partial Regex PhoneSeparatorsRegex();

    [GeneratedRegex(@"^\+?\d{8,15}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"[\s\-]")]
    private static partial Regex IdentificationSeparatorsRegex();

    [GeneratedRegex("^[0-9]{1,19}[0-9K]$")]
    private static partial Regex NitRegex();
}
