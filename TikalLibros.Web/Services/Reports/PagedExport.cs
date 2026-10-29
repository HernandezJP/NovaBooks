using System.Text.Json;
using TikalLibros.Application.Common;
using TikalLibros.Web.Services.Api;

namespace TikalLibros.Web.Services.Reports;

/// <summary>
/// Junta todas las páginas de una lista paginada (con los filtros vigentes)
/// para exportarla, hasta <see cref="ExportTable.MaxRows"/> filas.
/// </summary>
public static class PagedExport
{
    public const int PageSize = 100;

    /// <summary>Copia de la consulta de la pantalla (con sus filtros) para paginarla sin alterar la vista.</summary>
    public static T Copy<T>(T query) where T : class =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(query))!;

    /// <summary>Todas las filas; si alguna página falla lanza <see cref="ExportFailedException"/> con el mensaje de la API.</summary>
    public static async Task<List<T>> AllAsync<T>(
        Func<int, int, Task<ApiResult<PagedResponse<T>>>> fetchPage)
    {
        List<T> items = [];

        for (int page = 1; items.Count < ExportTable.MaxRows; page++)
        {
            ApiResult<PagedResponse<T>> result = await fetchPage(page, PageSize);

            if (!result.Succeeded)
            {
                throw new ExportFailedException(result.Message);
            }

            items.AddRange(result.Data!.Items);

            if (page >= result.Data.TotalPages || result.Data.Items.Count == 0)
            {
                break;
            }
        }

        return items.Take(ExportTable.MaxRows).ToList();
    }
}

/// <summary>No fue posible obtener los datos a exportar; el mensaje se muestra al usuario.</summary>
public sealed class ExportFailedException(string message) : Exception(message);
