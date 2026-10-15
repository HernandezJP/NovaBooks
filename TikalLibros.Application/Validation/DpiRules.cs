namespace TikalLibros.Application.Validation;

/// <summary>
/// Validación del DPI (CUI) de Guatemala: 13 dígitos = 8 de correlativo,
/// 1 verificador, 2 de departamento y 2 de municipio.
/// </summary>
public static class DpiRules
{
    // Municipios por departamento (01 Guatemala … 22 Jutiapa).
    private static readonly int[] MunicipalitiesByDepartment =
        [17, 8, 16, 16, 13, 14, 19, 8, 24, 21, 9, 30, 32, 21, 8, 17, 14, 5, 11, 11, 7, 17];

    /// <summary>Quita espacios y guiones ("2345 67890 0101").</summary>
    public static string Normalize(string value)
    {
        return new string(value.Where(character => !char.IsWhiteSpace(character) && character != '-').ToArray());
    }

    public static bool IsValid(string normalizedDpi)
    {
        if (normalizedDpi.Length != 13 || !normalizedDpi.All(char.IsAsciiDigit))
        {
            return false;
        }

        int department = int.Parse(normalizedDpi.AsSpan(9, 2));
        int municipality = int.Parse(normalizedDpi.AsSpan(11, 2));

        if (department < 1 || department > MunicipalitiesByDepartment.Length ||
            municipality < 1 || municipality > MunicipalitiesByDepartment[department - 1])
        {
            return false;
        }

        int total = 0;

        for (int index = 0; index < 8; index++)
        {
            total += (normalizedDpi[index] - '0') * (index + 2);
        }

        return total % 11 == normalizedDpi[8] - '0';
    }
}
