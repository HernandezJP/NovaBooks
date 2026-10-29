using System.Globalization;
using ClosedXML.Excel;
using Microsoft.JSInterop;

namespace TikalLibros.Web.Services.Reports;

/// <summary>Tipo de una columna exportada: define el formato en CSV y en Excel.</summary>
public enum ExportKind
{
    Text,
    Number,
    Money,
    Percent,
    Date,
    DateTime
}

public sealed record ExportColumn(string Header, ExportKind Kind = ExportKind.Text);

/// <summary>
/// Tabla lista para exportar: título y subtítulo (por ejemplo, el período),
/// columnas tipadas y filas. La misma tabla sirve para CSV y para Excel.
/// </summary>
public sealed record ExportTable(
    string FileName,
    string Title,
    string? Subtitle,
    IReadOnlyList<ExportColumn> Columns,
    IReadOnlyList<object?[]> Rows)
{
    /// <summary>Filas como máximo en una exportación (las listas se descargan completas hasta este tope).</summary>
    public const int MaxRows = 5000;
}

public static class TableExport
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static ValueTask DownloadCsvAsync(IJSRuntime js, ExportTable table) =>
        CsvExport.DownloadAsync(js, $"{table.FileName}.csv", CsvExport.Build(table.Columns.Select(column => column.Header), table.Rows));

    public static async Task DownloadExcelAsync(IJSRuntime js, ExportTable table)
    {
        using MemoryStream stream = new(ToExcel(table));
        using DotNetStreamReference reference = new(stream);
        await js.InvokeVoidAsync("tikalLibros.downloadStream", $"{table.FileName}.xlsx", reference, XlsxMime);
    }

    /// <summary>
    /// Libro de Excel con título, subtítulo, encabezados con filtro, paneles
    /// inmovilizados y formatos por tipo de columna. Los textos se guardan como
    /// texto (nunca como fórmula).
    /// </summary>
    public static byte[] ToExcel(ExportTable table)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add(SheetName(table.Title));

        int columns = Math.Max(table.Columns.Count, 1);

        sheet.Cell(1, 1).Value = table.Title;
        sheet.Range(1, 1, 1, columns).Merge().Style.Font.SetBold().Font.SetFontSize(14);

        string subtitle = $"{table.Subtitle}{(string.IsNullOrEmpty(table.Subtitle) ? "" : " · ")}Generado el {DateTime.Now:dd/MM/yyyy HH:mm} · Tikal Libros";
        sheet.Cell(2, 1).Value = subtitle;
        sheet.Range(2, 1, 2, columns).Merge().Style.Font.SetFontColor(XLColor.FromHtml("#5F6673"));

        const int headerRow = 4;

        for (int column = 0; column < table.Columns.Count; column++)
        {
            IXLCell cell = sheet.Cell(headerRow, column + 1);
            cell.Value = table.Columns[column].Header;
            cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1F3049"));
        }

        for (int row = 0; row < table.Rows.Count; row++)
        {
            object?[] values = table.Rows[row];

            for (int column = 0; column < table.Columns.Count; column++)
            {
                IXLCell cell = sheet.Cell(headerRow + 1 + row, column + 1);
                SetValue(cell, column < values.Length ? values[column] : null, table.Columns[column].Kind);
            }
        }

        if (table.Columns.Count > 0)
        {
            sheet.Range(headerRow, 1, headerRow + Math.Max(table.Rows.Count, 1), table.Columns.Count).SetAutoFilter();
            sheet.SheetView.FreezeRows(headerRow);
            sheet.Columns(1, table.Columns.Count).AdjustToContents(headerRow, headerRow + Math.Min(table.Rows.Count, 500));

            foreach (IXLColumn column in sheet.Columns(1, table.Columns.Count))
            {
                column.Width = Math.Min(Math.Max(column.Width, 10), 60);
            }
        }

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetValue(IXLCell cell, object? value, ExportKind kind)
    {
        switch (value)
        {
            case null:
                return;
            case decimal number:
                cell.Value = number;
                break;
            case int number:
                cell.Value = number;
                break;
            case double number:
                cell.Value = number;
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                break;
            case DateTime date:
                cell.Value = kind == ExportKind.Date ? date.ToLocalTime().Date : date.ToLocalTime();
                break;
            case bool flag:
                cell.Value = flag ? "Sí" : "No";
                break;
            default:
                // Texto literal: Excel no lo interpreta como fórmula ni como número.
                cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                cell.Style.NumberFormat.Format = "@";
                return;
        }

        cell.Style.NumberFormat.Format = kind switch
        {
            ExportKind.Money => "\"Q\" #,##0.00",
            ExportKind.Number => "#,##0.##",
            ExportKind.Percent => "0.0\" %\"",
            ExportKind.Date => "dd/mm/yyyy",
            ExportKind.DateTime => "dd/mm/yyyy hh:mm",
            _ => cell.Style.NumberFormat.Format
        };
    }

    /// <summary>Nombre de hoja válido: máximo 31 caracteres y sin \ / ? * [ ] :.</summary>
    private static string SheetName(string title)
    {
        string name = new(title.Where(character => "\\/?*[]:".IndexOf(character) < 0).ToArray());
        name = name.Trim();
        return name.Length == 0 ? "Datos" : name[..Math.Min(31, name.Length)];
    }
}
