extern alias web;

using System.Net;
using Bunit;
using ClosedXML.Excel;
using TikalLibros.Application.Common;
using web::TikalLibros.Web.Components.Shared;
using web::TikalLibros.Web.Services.Api;
using web::TikalLibros.Web.Services.Reports;

namespace TikalLibros.IntegrationTests.Web;

/// <summary>Exportación a CSV y Excel, listas completas y botones de exportación.</summary>
public sealed class ExportTests : WebTestContext
{
    private static ExportTable Sample(int rows = 2) => new(
        "ventas-prueba",
        "Ventas: prueba/octubre",
        "Del 01/10/2026 al 27/10/2026",
        [new("Venta"), new("Fecha", ExportKind.Date), new("Total", ExportKind.Money), new("Unidades", ExportKind.Number), new("Anulada")],
        Enumerable.Range(1, rows)
            .Select(index => new object?[] { index == 1 ? "=HYPERLINK(\"x\")" : $"VEN-{index:000000}", new DateOnly(2026, 10, index), 112.5m * index, index, false })
            .ToList());

    [Fact]
    public void Excel_TieneTituloEncabezadosTiposYFormatos()
    {
        using XLWorkbook workbook = new(new MemoryStream(TableExport.ToExcel(Sample())));
        IXLWorksheet sheet = workbook.Worksheets.Single();

        Assert.Equal("Ventas pruebaoctubre", sheet.Name); // sin los caracteres que Excel no admite en el nombre
        Assert.Equal("Ventas: prueba/octubre", sheet.Cell(1, 1).GetString());
        Assert.StartsWith("Del 01/10/2026 al 27/10/2026 · Generado el", sheet.Cell(2, 1).GetString());

        Assert.Equal("Total", sheet.Cell(4, 3).GetString());
        Assert.True(sheet.Cell(4, 3).Style.Font.Bold);

        // Un texto que parece fórmula se guarda como texto.
        Assert.False(sheet.Cell(5, 1).HasFormula);
        Assert.Equal("=HYPERLINK(\"x\")", sheet.Cell(5, 1).GetString());

        Assert.Equal(XLDataType.DateTime, sheet.Cell(5, 2).DataType);
        Assert.Equal(new DateTime(2026, 10, 1), sheet.Cell(5, 2).GetDateTime());
        Assert.Equal(XLDataType.Number, sheet.Cell(6, 3).DataType);
        Assert.Equal(225m, (decimal)sheet.Cell(6, 3).GetDouble());
        Assert.Contains("#,##0.00", sheet.Cell(6, 3).Style.NumberFormat.Format);
        Assert.Equal("No", sheet.Cell(5, 5).GetString());

        Assert.NotNull(sheet.AutoFilter);
        Assert.Equal(4, sheet.SheetView.SplitRow);
    }

    [Fact]
    public void Csv_EscapaComasYFormulas()
    {
        string csv = CsvExport.Build(["Libro", "Total"], [new object?[] { "Rayuela, edición \"especial\"", 10.5m }, new object?[] { "=1+1", 2m }]);

        Assert.Contains("\"Rayuela, edición \"\"especial\"\"\",10.5", csv);
        Assert.Contains("'=1+1,2", csv);
    }

    [Fact]
    public async Task ListasCompletas_JuntaTodasLasPaginasHastaElTope()
    {
        List<int> requested = [];

        List<int> all = await PagedExport.AllAsync((page, size) =>
        {
            requested.Add(page);
            List<int> items = Enumerable.Range((page - 1) * size, page == 3 ? 20 : size).ToList();
            return Task.FromResult(ApiResultOf(new PagedResponse<int> { Items = items, Page = page, PageSize = size, TotalItems = 220 }));
        });

        Assert.Equal([1, 2, 3], requested);
        Assert.Equal(220, all.Count);

        // Con muchas páginas se detiene en el tope de filas.
        List<int> capped = await PagedExport.AllAsync((page, size) =>
            Task.FromResult(ApiResultOf(new PagedResponse<int> { Items = Enumerable.Range(0, size).ToList(), Page = page, PageSize = size, TotalItems = 100_000 })));
        Assert.Equal(ExportTable.MaxRows, capped.Count);
    }

    [Fact]
    public async Task ListasCompletas_SiUnaPaginaFallaLanzaElMensaje()
    {
        ExportFailedException error = await Assert.ThrowsAsync<ExportFailedException>(() =>
            PagedExport.AllAsync<int>((_, _) => Task.FromResult(ApiResult<PagedResponse<int>>.Fail(ApiFailure.Validation, "Sin conexión con la API."))));

        Assert.Equal("Sin conexión con la API.", error.Message);
    }

    [Fact]
    public async Task Botones_DescarganCsvYExcel()
    {
        await Session.SignInAsync(ValidLogin("Reportes.Ventas"));
        JSInterop.SetupVoid("tikalLibros.downloadText", _ => true).SetVoidResult();
        JSInterop.SetupVoid("tikalLibros.downloadStream", _ => true).SetVoidResult();

        IRenderedComponent<ExportActions> actions = Render<ExportActions>(parameters => parameters
            .Add(p => p.Build, () => Task.FromResult<ExportTable?>(Sample())));

        await actions.FindAll("button").Single(button => button.TextContent.Contains("CSV")).ClickAsync(new());
        await actions.FindAll("button").Single(button => button.TextContent.Contains("Excel")).ClickAsync(new());

        JSRuntimeInvocation csv = JSInterop.Invocations.Single(call => call.Identifier == "tikalLibros.downloadText");
        Assert.Equal("ventas-prueba.csv", csv.Arguments[0]);
        Assert.Contains("VEN-000002", (string)csv.Arguments[1]!);

        JSRuntimeInvocation excel = JSInterop.Invocations.Single(call => call.Identifier == "tikalLibros.downloadStream");
        Assert.Equal("ventas-prueba.xlsx", excel.Arguments[0]);
        Assert.Contains("Imprimir / PDF", actions.Markup);
    }

    private static ApiResult<PagedResponse<int>> ApiResultOf(PagedResponse<int> page) => ApiResult<PagedResponse<int>>.Success(page, 200);
}
