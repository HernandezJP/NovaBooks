namespace NovaBooks.Application.Validation;

/// <summary>
/// Normalización y validación de ISBN-10 e ISBN-13 con dígito de control.
/// </summary>
public static class IsbnRules
{
    /// <summary>
    /// Quita espacios y guiones; la X final del ISBN-10 queda en mayúscula.
    /// </summary>
    public static string Normalize(string value)
    {
        return new string(value
                .Where(character => character is not (' ' or '-'))
                .ToArray())
            .ToUpperInvariant();
    }

    public static bool IsValidIsbn10(string normalized)
    {
        if (normalized.Length != 10)
        {
            return false;
        }

        int sum = 0;

        for (int index = 0; index < 10; index++)
        {
            char character = normalized[index];
            int digit;

            if (char.IsAsciiDigit(character))
            {
                digit = character - '0';
            }
            else if (character == 'X' && index == 9)
            {
                digit = 10;
            }
            else
            {
                return false;
            }

            sum += digit * (10 - index);
        }

        return sum % 11 == 0;
    }

    public static bool IsValidIsbn13(string normalized)
    {
        if (normalized.Length != 13 ||
            !normalized.All(char.IsAsciiDigit) ||
            !(normalized.StartsWith("978", StringComparison.Ordinal) ||
              normalized.StartsWith("979", StringComparison.Ordinal)))
        {
            return false;
        }

        int sum = 0;

        for (int index = 0; index < 13; index++)
        {
            int digit = normalized[index] - '0';
            sum += index % 2 == 0 ? digit : digit * 3;
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// Convierte un ISBN-10 válido a su ISBN-13 (prefijo 978).
    /// </summary>
    public static string ToIsbn13(string isbn10)
    {
        string body = "978" + isbn10[..9];
        int sum = 0;

        for (int index = 0; index < 12; index++)
        {
            int digit = body[index] - '0';
            sum += index % 2 == 0 ? digit : digit * 3;
        }

        int check = (10 - sum % 10) % 10;

        return body + check;
    }
}
