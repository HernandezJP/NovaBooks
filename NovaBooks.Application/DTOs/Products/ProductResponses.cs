using NovaBooks.Application.DTOs.Common;

namespace NovaBooks.Application.DTOs.Products;

public sealed class ProductListItemResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? Isbn10 { get; set; }

    public string? Isbn13 { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Authors { get; set; } = string.Empty;

    public string? PrimaryCategory { get; set; }

    public string Format { get; set; } = string.Empty;

    /// <summary>Precio vigente en la lista predeterminada.</summary>
    public decimal? Price { get; set; }

    /// <summary>Solo con Catalogo.AdministrarPrecios.</summary>
    public decimal? ReferenceCost { get; set; }

    public bool AllowsSale { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Versión de la portada; null si no tiene imagen.</summary>
    public string? ImageVersion { get; set; }
}

public sealed class ProductResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? Isbn10 { get; set; }

    public string? Isbn13 { get; set; }

    public string? Barcode { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public int? EditorialId { get; set; }

    public string? EditorialName { get; set; }

    public int LanguageId { get; set; }

    public string LanguageName { get; set; } = string.Empty;

    public int FormatId { get; set; }

    public string FormatName { get; set; } = string.Empty;

    public int TaxId { get; set; }

    public string TaxName { get; set; } = string.Empty;

    public string? Edition { get; set; }

    public int? PublicationYear { get; set; }

    public int? PageCount { get; set; }

    public string? Description { get; set; }

    public bool AllowsSale { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Solo con Catalogo.AdministrarPrecios.</summary>
    public decimal? ReferenceCost { get; set; }

    /// <summary>Versión de la portada; null si no tiene imagen.</summary>
    public string? ImageVersion { get; set; }

    public IReadOnlyCollection<ProductAuthorResponse> Authors { get; set; } =
        Array.Empty<ProductAuthorResponse>();

    public IReadOnlyCollection<ProductCategoryResponse> Categories { get; set; } =
        Array.Empty<ProductCategoryResponse>();

    /// <summary>Precios vigentes por lista.</summary>
    public IReadOnlyCollection<ProductPriceResponse> Prices { get; set; } =
        Array.Empty<ProductPriceResponse>();

    /// <summary>Precios anteriores, del más reciente al más antiguo.</summary>
    public IReadOnlyCollection<ProductPriceResponse> PriceHistory { get; set; } =
        Array.Empty<ProductPriceResponse>();

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public sealed class ProductAuthorResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Order { get; set; }
}

public sealed class ProductCategoryResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}

public sealed class ProductPriceResponse
{
    public int PriceListId { get; set; }

    public string PriceListName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public sealed class ChangeProductStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class ProductQuery
{
    /// <summary>Código, ISBN, código de barras, título, autor o categoría.</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public int? CategoryId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class ProductCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> Editorials { get; set; } =
        Array.Empty<CatalogOptionResponse>();

    public IReadOnlyCollection<CatalogOptionResponse> Languages { get; set; } =
        Array.Empty<CatalogOptionResponse>();

    public IReadOnlyCollection<CatalogOptionResponse> Formats { get; set; } =
        Array.Empty<CatalogOptionResponse>();

    public IReadOnlyCollection<TaxOptionResponse> Taxes { get; set; } =
        Array.Empty<TaxOptionResponse>();

    public IReadOnlyCollection<CatalogOptionResponse> Authors { get; set; } =
        Array.Empty<CatalogOptionResponse>();

    public IReadOnlyCollection<CatalogOptionResponse> Categories { get; set; } =
        Array.Empty<CatalogOptionResponse>();

    public IReadOnlyCollection<PriceListOptionResponse> PriceLists { get; set; } =
        Array.Empty<PriceListOptionResponse>();
}

public sealed class TaxOptionResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Percentage { get; set; }
}

public sealed class PriceListOptionResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
