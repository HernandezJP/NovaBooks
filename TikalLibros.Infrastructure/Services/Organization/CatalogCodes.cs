using System.Globalization;
using System.Text;

namespace TikalLibros.Infrastructure.Services.Organization;

/// <summary>
/// Códigos que asigna el sistema: a partir del nombre ("Ciencia ficción" →
/// CIENCIA_FICCION) o correlativos con prefijo (SUC-02, ALM-03).
/// </summary>
public static class CatalogCodes
{
    public static string FromName(string name, int maxLength)
    {
        StringBuilder code = new();

        foreach (char character in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                code.Append(char.ToUpperInvariant(character));
            }
            else if (code.Length > 0 && code[^1] != '_')
            {
                code.Append('_');
            }
        }

        string value = code.ToString().Trim('_');
        value = value.Length > maxLength ? value[..maxLength].TrimEnd('_') : value;

        return value.Length == 0 ? "CAT" : value;
    }

    /// <summary>Agrega _2, _3… si el código ya está en uso.</summary>
    public static string MakeUnique(string code, int maxLength, IReadOnlyCollection<string> existing)
    {
        HashSet<string> used = new(existing, StringComparer.OrdinalIgnoreCase);

        if (!used.Contains(code))
        {
            return code;
        }

        for (int suffix = 2; ; suffix++)
        {
            string tail = $"_{suffix}";
            string candidate = (code.Length + tail.Length > maxLength ? code[..(maxLength - tail.Length)] : code) + tail;

            if (!used.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    /// <summary>Siguiente correlativo: SUC-01, SUC-02… (ignora códigos con otro formato).</summary>
    public static string NextSequential(string prefix, IEnumerable<string> existing)
    {
        int max = existing
            .Where(code => code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(code => int.TryParse(code.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{max + 1:D2}";
    }

    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
