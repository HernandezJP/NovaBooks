using System.Globalization;
using System.Text;

namespace NovaBooks.Web.Components.Shared;

/// <summary>
/// Búsqueda en listas de la interfaz sin distinguir mayúsculas ni tildes
/// ("garcia" encuentra "García Márquez").
/// </summary>
public static class SearchText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);

        foreach (char character in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    public static bool Matches(string? text, string? search)
    {
        string term = Normalize(search);

        return term.Length == 0 || Normalize(text).Contains(term, StringComparison.Ordinal);
    }
}
