using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Products;
using TikalLibros.Web.Security;
using TikalLibros.Web.Services.Session;

namespace TikalLibros.Web.Services.Api;

public sealed record BookOption(int Id, string Code, string Title, string? Isbn);

/// <summary>
/// Búsqueda de libros para las pantallas de inventario: usa el catálogo si
/// el usuario puede verlo; si no, los libros que ya tienen existencia.
/// </summary>
public sealed class BookLookup(ProductsApi products, InventoryApi inventory, SessionState session)
{
    public async Task<IEnumerable<BookOption>> SearchAsync(string? text, CancellationToken cancellationToken)
    {
        if (session.HasPermission(AppPermissions.CatalogView))
        {
            ApiResult<PagedResponse<ProductListItemResponse>> books =
                await products.GetPagedAsync(text, StatusFilter.Active, null, 1, 15, cancellationToken);

            return books.Data?.Items.Select(book => new BookOption(book.Id, book.Code, book.Title, book.Isbn13 ?? book.Isbn10)) ?? [];
        }

        ApiResult<PagedResponse<StockItemResponse>> stock =
            await inventory.GetStockAsync(new StockQuery { Search = text, PageSize = 15 }, cancellationToken);

        return stock.Data?.Items
            .DistinctBy(item => item.BookId)
            .Select(item => new BookOption(item.BookId, item.BookCode, item.Title, item.Isbn)) ?? [];
    }
}
