using System.Globalization;
using System.Text;
using Microsoft.JSInterop;

namespace TikalLibros.Web.Services.Reports;

/// <summary>
/// Exporta filas a CSV (separado por comas, números con punto decimal) y lo
/// descarga en el navegador. Los textos se citan para que comas, comillas y
/// saltos de línea no rompan las columnas; las celdas que empiezan con = + -
/// o @ se anteponen con un apóstrofo para que Excel no las ejecute como fórmula.
/// </summary>
public static class CsvExport
{
    public static string Build(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        StringBuilder builder = new();
        builder.AppendLine(string.Join(",", headers.Select(Escape)));

        foreach (IEnumerable<object?> row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Format)));
        }

        return builder.ToString();
    }

    public static ValueTask DownloadAsync(IJSRuntime js, string fileName, string content) =>
        js.InvokeVoidAsync("tikalLibros.downloadText", fileName, content, "text/csv;charset=utf-8");

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        decimal number => number.ToString("0.##", CultureInfo.InvariantCulture),
        int number => number.ToString(CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime date => date.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        bool flag => flag ? "Sí" : "No",
        _ => Escape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
    };

    private static string Escape(string text)
    {
        if (text.Length > 0 && "=+-@".Contains(text[0]))
        {
            text = "'" + text;
        }

        return text.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }
}
