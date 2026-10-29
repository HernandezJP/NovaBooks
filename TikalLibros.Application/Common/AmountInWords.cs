namespace TikalLibros.Application.Common;

/// <summary>
/// Monto en letras para facturas: "Doscientos doce quetzales con 80/100".
/// Admite hasta 999,999,999.99.
/// </summary>
public static class AmountInWords
{
    private static readonly string[] Units =
    [
        "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
        "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve",
        "veinte", "veintiuno", "veintidós", "veintitrés", "veinticuatro", "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve"
    ];

    private static readonly string[] Tens =
        ["", "", "", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"];

    private static readonly string[] Hundreds =
        ["", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"];

    public static string Quetzales(decimal amount)
    {
        if (amount < 0 || amount >= 1_000_000_000m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Monto fuera del rango admitido.");
        }

        decimal rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        long whole = (long)decimal.Truncate(rounded);
        int cents = (int)((rounded - whole) * 100);

        // "uno" se apocopa delante del sustantivo: "un quetzal", "veintiún quetzales".
        string words = whole == 0 ? "cero" : Apocope(Number(whole));
        string currency = whole == 1 ? "quetzal" : "quetzales";
        string text = $"{words} {currency} con {cents:00}/100";

        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static string Number(long value)
    {
        if (value >= 1_000_000)
        {
            long millions = value / 1_000_000;
            long rest = value % 1_000_000;
            string head = millions == 1 ? "un millón" : $"{Apocope(Number(millions))} millones";
            return rest == 0 ? head : $"{head} {Number(rest)}";
        }

        if (value >= 1000)
        {
            long thousands = value / 1000;
            long rest = value % 1000;
            string head = thousands == 1 ? "mil" : $"{Apocope(Number(thousands))} mil";
            return rest == 0 ? head : $"{head} {Number(rest)}";
        }

        if (value >= 100)
        {
            long rest = value % 100;
            return value == 100 ? "cien" : rest == 0 ? Hundreds[value / 100] : $"{Hundreds[value / 100]} {Number(rest)}";
        }

        if (value < 30)
        {
            return Units[value];
        }

        long unit = value % 10;
        return unit == 0 ? Tens[value / 10] : $"{Tens[value / 10]} y {Units[unit]}";
    }

    // "veintiuno mil" → "veintiún mil"; "treinta y uno mil" → "treinta y un mil".
    private static string Apocope(string words) =>
        words.EndsWith("veintiuno", StringComparison.Ordinal) ? words[..^9] + "veintiún"
        : words.EndsWith("uno", StringComparison.Ordinal) ? words[..^3] + "un"
        : words;
}
