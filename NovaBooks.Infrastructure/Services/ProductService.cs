using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Common;
using NovaBooks.Application.DTOs.Products;
using NovaBooks.Application.Interfaces;
using NovaBooks.Application.Validation;
using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Infrastructure.Data;

namespace NovaBooks.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private const string CodePrefix = "LIB-";

    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<ProductListItemResponse>> GetPagedAsync(
        ProductQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;

        int pageSize = query.PageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => query.PageSize
        };

        IQueryable<PB_LIBRO> books = _context.Libros.AsNoTracking();

        if (query.IsActive.HasValue)
        {
            books = books.Where(book => book.LIB_Activo == query.IsActive.Value);
        }

        if (query.CategoryId.HasValue)
        {
            books = books.Where(book => book.LibrosCategorias.Any(link =>
                link.LCA_CategoriaId == query.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            string isbn = IsbnRules.Normalize(value);

            if (isbn.Length == 0)
            {
                isbn = value;
            }

            books = books.Where(book =>
                book.LIB_Codigo.Contains(value) ||
                (book.LIB_ISBN10 != null && book.LIB_ISBN10.Contains(isbn)) ||
                (book.LIB_ISBN13 != null && book.LIB_ISBN13.Contains(isbn)) ||
                (book.LIB_CodigoBarras != null && book.LIB_CodigoBarras.Contains(value)) ||
                book.LIB_Titulo.Contains(value) ||
                (book.LIB_Subtitulo != null && book.LIB_Subtitulo.Contains(value)) ||
                book.LibrosAutores.Any(link =>
                    link.Autor.AUT_PrimerNombre.Contains(value) ||
                    link.Autor.AUT_PrimerApellido.Contains(value) ||
                    (link.Autor.AUT_PrimerNombre + " " + link.Autor.AUT_PrimerApellido)
                        .Contains(value) ||
                    (link.Autor.AUT_Seudonimo != null &&
                     link.Autor.AUT_Seudonimo.Contains(value))) ||
                book.LibrosCategorias.Any(link =>
                    link.Categoria.CAT_Nombre.Contains(value)));
        }

        int totalItems = await books.CountAsync(cancellationToken);

        var rows =
            await books
                .OrderBy(book => book.LIB_Titulo)
                .ThenBy(book => book.LIB_Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(book => new
                {
                    book.LIB_Libro,
                    book.LIB_Codigo,
                    book.LIB_ISBN10,
                    book.LIB_ISBN13,
                    book.LIB_Titulo,
                    book.LIB_Subtitulo,
                    Format = book.FormatoLibro.FLI_Nombre,
                    PrimaryCategory = book.LibrosCategorias
                        .OrderByDescending(link => link.LCA_Principal)
                        .Select(link => link.Categoria.CAT_Nombre)
                        .FirstOrDefault(),
                    Price = book.Precios
                        .Where(price =>
                            price.LIP_Activo &&
                            price.LIP_FechaFin == null &&
                            price.ListaPrecio.LPR_EsPredeterminada)
                        .Select(price => (decimal?)price.LIP_Precio)
                        .FirstOrDefault(),
                    book.LIB_CostoReferencia,
                    book.LIB_PermiteVenta,
                    book.LIB_Activo,
                    book.LIB_RutaImagen
                })
                .ToListAsync(cancellationToken);

        int[] bookIds = rows.Select(row => row.LIB_Libro).ToArray();

        ILookup<int, string> authorsByBook =
            (await _context.LibrosAutores
                .AsNoTracking()
                .Where(link => bookIds.Contains(link.LAU_LibroId))
                .OrderBy(link => link.LAU_Orden)
                .Select(link => new
                {
                    link.LAU_LibroId,
                    link.Autor.AUT_Seudonimo,
                    link.Autor.AUT_PrimerNombre,
                    link.Autor.AUT_PrimerApellido
                })
                .ToListAsync(cancellationToken))
            .ToLookup(
                link => link.LAU_LibroId,
                link => AuthorName(
                    link.AUT_Seudonimo,
                    link.AUT_PrimerNombre,
                    link.AUT_PrimerApellido));

        return new PagedResponse<ProductListItemResponse>
        {
            Items = rows
                .Select(row => new ProductListItemResponse
                {
                    Id = row.LIB_Libro,
                    Code = row.LIB_Codigo,
                    Isbn10 = row.LIB_ISBN10,
                    Isbn13 = row.LIB_ISBN13,
                    Title = row.LIB_Titulo,
                    Subtitle = row.LIB_Subtitulo,
                    Authors = string.Join(", ", authorsByBook[row.LIB_Libro]),
                    PrimaryCategory = row.PrimaryCategory,
                    Format = row.Format,
                    Price = row.Price,
                    ReferenceCost = includeCosts ? row.LIB_CostoReferencia : null,
                    AllowsSale = row.LIB_PermiteVenta,
                    IsActive = row.LIB_Activo,
                    ImageVersion = row.LIB_RutaImagen
                })
                .ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ProductResponse?> GetByIdAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        ProductResponse? response =
            await _context.Libros
                .AsNoTracking()
                .Where(book => book.LIB_Libro == id)
                .Select(book => new ProductResponse
                {
                    Id = book.LIB_Libro,
                    Code = book.LIB_Codigo,
                    Isbn10 = book.LIB_ISBN10,
                    Isbn13 = book.LIB_ISBN13,
                    Barcode = book.LIB_CodigoBarras,
                    Title = book.LIB_Titulo,
                    Subtitle = book.LIB_Subtitulo,
                    EditorialId = book.LIB_EditorialId,
                    EditorialName = book.Editorial != null
                        ? book.Editorial.EDI_Nombre
                        : null,
                    LanguageId = book.LIB_IdiomaId,
                    LanguageName = book.Idioma.IDI_Nombre,
                    FormatId = book.LIB_FormatoLibroId,
                    FormatName = book.FormatoLibro.FLI_Nombre,
                    TaxId = book.LIB_ImpuestoId,
                    TaxName = book.Impuesto.IMP_Nombre,
                    Edition = book.LIB_NumeroEdicion,
                    PublicationYear = book.LIB_AnioPublicacion,
                    PageCount = book.LIB_NumeroPaginas,
                    Description = book.LIB_Descripcion,
                    AllowsSale = book.LIB_PermiteVenta,
                    IsActive = book.LIB_Activo,
                    ReferenceCost = includeCosts ? (decimal?)book.LIB_CostoReferencia : null,
                    ImageVersion = book.LIB_RutaImagen,
                    CreatedAt = book.LIB_FechaCreacion,
                    ModifiedAt = book.LIB_FechaModificacion
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return null;
        }

        response.Authors =
            (await _context.LibrosAutores
                .AsNoTracking()
                .Where(link => link.LAU_LibroId == id)
                .OrderBy(link => link.LAU_Orden)
                .Select(link => new
                {
                    link.LAU_AutorId,
                    link.LAU_Orden,
                    link.Autor.AUT_Seudonimo,
                    link.Autor.AUT_PrimerNombre,
                    link.Autor.AUT_PrimerApellido
                })
                .ToListAsync(cancellationToken))
            .Select(link => new ProductAuthorResponse
            {
                Id = link.LAU_AutorId,
                Order = link.LAU_Orden,
                Name = AuthorName(
                    link.AUT_Seudonimo,
                    link.AUT_PrimerNombre,
                    link.AUT_PrimerApellido)
            })
            .ToArray();

        response.Categories =
            await _context.LibrosCategorias
                .AsNoTracking()
                .Where(link => link.LCA_LibroId == id)
                .OrderByDescending(link => link.LCA_Principal)
                .ThenBy(link => link.Categoria.CAT_Nombre)
                .Select(link => new ProductCategoryResponse
                {
                    Id = link.LCA_CategoriaId,
                    Name = link.Categoria.CAT_Nombre,
                    IsPrimary = link.LCA_Principal
                })
                .ToListAsync(cancellationToken);

        List<ProductPriceResponse> prices =
            await _context.LibrosPrecios
                .AsNoTracking()
                .Where(price => price.LIP_LibroId == id)
                .OrderByDescending(price => price.LIP_FechaInicio)
                .Select(price => new ProductPriceResponse
                {
                    PriceListId = price.LIP_ListaPrecioId,
                    PriceListName = price.ListaPrecio.LPR_Nombre,
                    Price = price.LIP_Precio,
                    StartDate = price.LIP_FechaInicio,
                    EndDate = price.LIP_FechaFin
                })
                .ToListAsync(cancellationToken);

        response.Prices = prices
            .Where(price => price.EndDate is null)
            .OrderBy(price => price.PriceListName)
            .ToArray();

        response.PriceHistory = prices
            .Where(price => price.EndDate is not null)
            .ToArray();

        return response;
    }

    public async Task<ProductCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        return new ProductCatalogsResponse
        {
            Editorials =
                await _context.Editoriales
                    .AsNoTracking()
                    .Where(item => item.EDI_Activo)
                    .OrderBy(item => item.EDI_Nombre)
                    .Select(item => new CatalogOptionResponse
                    {
                        Id = item.EDI_Editorial,
                        Code = item.EDI_Codigo,
                        Name = item.EDI_Nombre
                    })
                    .ToListAsync(cancellationToken),

            Languages =
                await _context.Idiomas
                    .AsNoTracking()
                    .Where(item => item.IDI_Activo)
                    .OrderBy(item => item.IDI_Nombre)
                    .Select(item => new CatalogOptionResponse
                    {
                        Id = item.IDI_Idioma,
                        Code = item.IDI_Codigo,
                        Name = item.IDI_Nombre
                    })
                    .ToListAsync(cancellationToken),

            Formats =
                await _context.FormatosLibro
                    .AsNoTracking()
                    .Where(item => item.FLI_Activo)
                    .OrderBy(item => item.FLI_Nombre)
                    .Select(item => new CatalogOptionResponse
                    {
                        Id = item.FLI_FormatoLibro,
                        Code = item.FLI_Codigo,
                        Name = item.FLI_Nombre
                    })
                    .ToListAsync(cancellationToken),

            Taxes =
                await _context.Impuestos
                    .AsNoTracking()
                    .Where(item => item.IMP_Activo)
                    .OrderBy(item => item.IMP_Nombre)
                    .Select(item => new TaxOptionResponse
                    {
                        Id = item.IMP_Impuesto,
                        Code = item.IMP_Codigo,
                        Name = item.IMP_Nombre,
                        Percentage = item.IMP_Porcentaje
                    })
                    .ToListAsync(cancellationToken),

            Authors =
                (await _context.Autores
                    .AsNoTracking()
                    .Where(item => item.AUT_Activo)
                    .Select(item => new
                    {
                        item.AUT_Autor,
                        item.AUT_Seudonimo,
                        item.AUT_PrimerNombre,
                        item.AUT_PrimerApellido
                    })
                    .ToListAsync(cancellationToken))
                .Select(item => new CatalogOptionResponse
                {
                    Id = item.AUT_Autor,
                    Code = item.AUT_Autor.ToString(),
                    Name = AuthorName(
                        item.AUT_Seudonimo,
                        item.AUT_PrimerNombre,
                        item.AUT_PrimerApellido)
                })
                .OrderBy(item => item.Name)
                .ToArray(),

            Categories =
                await _context.Categorias
                    .AsNoTracking()
                    .Where(item => item.CAT_Activo)
                    .OrderBy(item => item.CAT_Nombre)
                    .Select(item => new CatalogOptionResponse
                    {
                        Id = item.CAT_Categoria,
                        Code = item.CAT_Codigo,
                        Name = item.CAT_Nombre
                    })
                    .ToListAsync(cancellationToken),

            PriceLists =
                await _context.ListasPrecio
                    .AsNoTracking()
                    .Where(item => item.LPR_Activo)
                    .OrderByDescending(item => item.LPR_EsPredeterminada)
                    .ThenBy(item => item.LPR_Nombre)
                    .Select(item => new PriceListOptionResponse
                    {
                        Id = item.LPR_ListaPrecio,
                        Code = item.LPR_Codigo,
                        Name = item.LPR_Nombre,
                        IsDefault = item.LPR_EsPredeterminada
                    })
                    .ToListAsync(cancellationToken)
        };
    }

    public async Task<OperationResult<ProductResponse>> CreateAsync(
        SaveProductRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        ProductInput input = ProductInput.From(request);

        string[] errors = await ValidateReferencesAsync(
            input,
            existing: null,
            cancellationToken);

        if (errors.Length > 0)
        {
            return OperationResult<ProductResponse>.Failure(errors);
        }

        // El código no se ingresa manualmente: siempre lo asigna el sistema.
        string code = await GenerateCodeAsync(cancellationToken);

        string? conflict = await FindConflictAsync(
            input,
            code,
            excludingBookId: null,
            cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<ProductResponse>.Conflict(conflict);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    DateTime now = DateTime.UtcNow;

                    PB_LIBRO book = new()
                    {
                        LIB_Codigo = code,
                        LIB_Activo = true,
                        LIB_FechaCreacion = now
                    };

                    ApplyBookData(book, input);

                    _context.Libros.Add(book);

                    await _context.SaveChangesAsync(token);

                    await SyncRelationsAsync(book.LIB_Libro, input, token);

                    SyncPrices(book.LIB_Libro, [], input.Prices, now);

                    await _context.SaveChangesAsync(token);

                    return OperationResult<ProductResponse>.Success(
                        (await GetByIdAsync(book.LIB_Libro, includeCosts, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<ProductResponse>> UpdateAsync(
        int id,
        SaveProductRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        PB_LIBRO? book =
            await _context.Libros.FirstOrDefaultAsync(
                item => item.LIB_Libro == id,
                cancellationToken);

        if (book is null)
        {
            return OperationResult<ProductResponse>.Missing(
                "El producto solicitado no existe.");
        }

        ProductInput input = ProductInput.From(request);

        string[] errors = await ValidateReferencesAsync(
            input,
            book,
            cancellationToken);

        if (errors.Length > 0)
        {
            return OperationResult<ProductResponse>.Failure(errors);
        }

        string code = book.LIB_Codigo;

        string? conflict = await FindConflictAsync(
            input,
            code,
            excludingBookId: book.LIB_Libro,
            cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<ProductResponse>.Conflict(conflict);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    DateTime now = DateTime.UtcNow;

                    book.LIB_Codigo = code;
                    book.LIB_FechaModificacion = now;

                    ApplyBookData(book, input);

                    await _context.SaveChangesAsync(token);

                    await SyncRelationsAsync(book.LIB_Libro, input, token);

                    if (input.Prices is not null)
                    {
                        List<PB_LIBRO_PRECIO> currentPrices =
                            await _context.LibrosPrecios
                                .Where(price =>
                                    price.LIP_LibroId == book.LIB_Libro &&
                                    price.LIP_Activo &&
                                    price.LIP_FechaFin == null)
                                .ToListAsync(token);

                        SyncPrices(book.LIB_Libro, currentPrices, input.Prices, now);
                    }

                    await _context.SaveChangesAsync(token);

                    return OperationResult<ProductResponse>.Success(
                        (await GetByIdAsync(book.LIB_Libro, includeCosts, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<ProductResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        PB_LIBRO? book =
            await _context.Libros.FirstOrDefaultAsync(
                item => item.LIB_Libro == id,
                cancellationToken);

        if (book is null)
        {
            return OperationResult<ProductResponse>.Missing(
                "El producto solicitado no existe.");
        }

        // Eliminación lógica: el libro se conserva para inventario,
        // compras, ventas e historial de precios.
        book.LIB_Activo = isActive;
        book.LIB_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<ProductResponse>.Success(
            (await GetByIdAsync(id, includeCosts, cancellationToken))!);
    }

    /// <summary>
    /// Un cambio de precio cierra el vigente (fecha fin) y crea uno nuevo;
    /// los precios históricos nunca se reescriben.
    /// </summary>
    private void SyncPrices(
        int bookId,
        List<PB_LIBRO_PRECIO> currentPrices,
        IReadOnlyCollection<ProductPriceRequest>? requestedPrices,
        DateTime now)
    {
        if (requestedPrices is null)
        {
            return;
        }

        foreach (ProductPriceRequest requested in requestedPrices)
        {
            PB_LIBRO_PRECIO? current = currentPrices.FirstOrDefault(price =>
                price.LIP_ListaPrecioId == requested.PriceListId);

            if (current is not null && current.LIP_Precio == requested.Price)
            {
                continue;
            }

            if (current is not null)
            {
                current.LIP_FechaFin = now;
                current.LIP_Activo = false;
            }

            _context.LibrosPrecios.Add(new PB_LIBRO_PRECIO
            {
                LIP_LibroId = bookId,
                LIP_ListaPrecioId = requested.PriceListId,
                LIP_Precio = requested.Price,
                LIP_FechaInicio = now,
                LIP_Activo = true,
                LIP_FechaCreacion = now
            });
        }
    }

    /// <summary>
    /// Reemplaza autores y categorías. Se guarda en dos pasos para
    /// respetar el índice único de categoría principal.
    /// </summary>
    private async Task SyncRelationsAsync(
        int bookId,
        ProductInput input,
        CancellationToken cancellationToken)
    {
        List<PB_LIBRO_AUTOR> authors =
            await _context.LibrosAutores
                .Where(link => link.LAU_LibroId == bookId)
                .ToListAsync(cancellationToken);

        List<PB_LIBRO_CATEGORIA> categories =
            await _context.LibrosCategorias
                .Where(link => link.LCA_LibroId == bookId)
                .ToListAsync(cancellationToken);

        _context.LibrosAutores.RemoveRange(authors.Where(link =>
            !input.AuthorIds.Contains(link.LAU_AutorId)));

        foreach (PB_LIBRO_CATEGORIA link in categories)
        {
            if (!input.CategoryIds.Contains(link.LCA_CategoriaId))
            {
                _context.LibrosCategorias.Remove(link);
            }
            else
            {
                link.LCA_Principal = false;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        for (int index = 0; index < input.AuthorIds.Count; index++)
        {
            int authorId = input.AuthorIds[index];

            PB_LIBRO_AUTOR? link = authors.FirstOrDefault(item =>
                item.LAU_AutorId == authorId);

            if (link is null)
            {
                _context.LibrosAutores.Add(new PB_LIBRO_AUTOR
                {
                    LAU_LibroId = bookId,
                    LAU_AutorId = authorId,
                    LAU_Orden = index + 1
                });
            }
            else
            {
                link.LAU_Orden = index + 1;
            }
        }

        foreach (int categoryId in input.CategoryIds)
        {
            bool isPrimary = categoryId == input.PrimaryCategoryId;

            PB_LIBRO_CATEGORIA? link = categories.FirstOrDefault(item =>
                item.LCA_CategoriaId == categoryId);

            if (link is null)
            {
                _context.LibrosCategorias.Add(new PB_LIBRO_CATEGORIA
                {
                    LCA_LibroId = bookId,
                    LCA_CategoriaId = categoryId,
                    LCA_Principal = isPrimary
                });
            }
            else
            {
                link.LCA_Principal = isPrimary;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Las referencias nuevas deben existir y estar activas; las que el
    /// libro ya tenía se aceptan aunque luego se hayan desactivado.
    /// </summary>
    private async Task<string[]> ValidateReferencesAsync(
        ProductInput input,
        PB_LIBRO? existing,
        CancellationToken cancellationToken)
    {
        List<string> errors = [];

        if (input.EditorialId.HasValue &&
            input.EditorialId != existing?.LIB_EditorialId &&
            !await _context.Editoriales.AnyAsync(
                item => item.EDI_Editorial == input.EditorialId && item.EDI_Activo,
                cancellationToken))
        {
            errors.Add("La editorial no existe o está inactiva.");
        }

        if (input.LanguageId != existing?.LIB_IdiomaId &&
            !await _context.Idiomas.AnyAsync(
                item => item.IDI_Idioma == input.LanguageId && item.IDI_Activo,
                cancellationToken))
        {
            errors.Add("El idioma no existe o está inactivo.");
        }

        if (input.FormatId != existing?.LIB_FormatoLibroId &&
            !await _context.FormatosLibro.AnyAsync(
                item => item.FLI_FormatoLibro == input.FormatId && item.FLI_Activo,
                cancellationToken))
        {
            errors.Add("El formato no existe o está inactivo.");
        }

        if (input.TaxId != existing?.LIB_ImpuestoId &&
            !await _context.Impuestos.AnyAsync(
                item => item.IMP_Impuesto == input.TaxId && item.IMP_Activo,
                cancellationToken))
        {
            errors.Add("El impuesto no existe o está inactivo.");
        }

        int? bookId = existing?.LIB_Libro;

        if (input.AuthorIds.Count > 0)
        {
            List<int> currentAuthors = bookId is null
                ? []
                : await _context.LibrosAutores
                    .Where(link => link.LAU_LibroId == bookId)
                    .Select(link => link.LAU_AutorId)
                    .ToListAsync(cancellationToken);

            List<int> validAuthors =
                await _context.Autores
                    .Where(item =>
                        input.AuthorIds.Contains(item.AUT_Autor) &&
                        (item.AUT_Activo || currentAuthors.Contains(item.AUT_Autor)))
                    .Select(item => item.AUT_Autor)
                    .ToListAsync(cancellationToken);

            if (validAuthors.Count != input.AuthorIds.Count)
            {
                errors.Add("Uno o más autores no existen o están inactivos.");
            }
        }

        if (input.CategoryIds.Count > 0)
        {
            List<int> currentCategories = bookId is null
                ? []
                : await _context.LibrosCategorias
                    .Where(link => link.LCA_LibroId == bookId)
                    .Select(link => link.LCA_CategoriaId)
                    .ToListAsync(cancellationToken);

            List<int> validCategories =
                await _context.Categorias
                    .Where(item =>
                        input.CategoryIds.Contains(item.CAT_Categoria) &&
                        (item.CAT_Activo || currentCategories.Contains(item.CAT_Categoria)))
                    .Select(item => item.CAT_Categoria)
                    .ToListAsync(cancellationToken);

            if (validCategories.Count != input.CategoryIds.Count)
            {
                errors.Add("Una o más categorías no existen o están inactivas.");
            }
        }

        if (input.Prices is { Count: > 0 })
        {
            int[] priceListIds = input.Prices
                .Select(price => price.PriceListId)
                .ToArray();

            int validLists = await _context.ListasPrecio.CountAsync(
                item => priceListIds.Contains(item.LPR_ListaPrecio) && item.LPR_Activo,
                cancellationToken);

            if (validLists != priceListIds.Length)
            {
                errors.Add("Una o más listas de precios no existen o están inactivas.");
            }
        }

        return errors.ToArray();
    }

    /// <summary>
    /// Busca duplicados también en libros inactivos.
    /// </summary>
    private async Task<string?> FindConflictAsync(
        ProductInput input,
        string code,
        int? excludingBookId,
        CancellationToken cancellationToken)
    {
        IQueryable<PB_LIBRO> others =
            _context.Libros
                .AsNoTracking()
                .Where(book => book.LIB_Libro != excludingBookId);

        var byCode = await others
            .Where(book => book.LIB_Codigo == code)
            .Select(book => new { book.LIB_Titulo, book.LIB_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (byCode is not null)
        {
            return $"El código {code} ya está registrado para \"{byCode.LIB_Titulo}\"" +
                   (byCode.LIB_Activo ? "." : " (inactivo).");
        }

        if (input.Isbn13 is not null)
        {
            var byIsbn13 = await others
                .Where(book => book.LIB_ISBN13 == input.Isbn13)
                .Select(book => new { book.LIB_Codigo, book.LIB_Activo })
                .FirstOrDefaultAsync(cancellationToken);

            if (byIsbn13 is not null)
            {
                return $"El ISBN-13 {input.Isbn13} ya está registrado en el producto " +
                       $"{byIsbn13.LIB_Codigo}" + (byIsbn13.LIB_Activo ? "." : " (inactivo).");
            }
        }

        if (input.Isbn10 is not null)
        {
            var byIsbn10 = await others
                .Where(book => book.LIB_ISBN10 == input.Isbn10)
                .Select(book => new { book.LIB_Codigo, book.LIB_Activo })
                .FirstOrDefaultAsync(cancellationToken);

            if (byIsbn10 is not null)
            {
                return $"El ISBN-10 {input.Isbn10} ya está registrado en el producto " +
                       $"{byIsbn10.LIB_Codigo}" + (byIsbn10.LIB_Activo ? "." : " (inactivo).");
            }
        }

        if (input.Barcode is not null)
        {
            var byBarcode = await others
                .Where(book => book.LIB_CodigoBarras == input.Barcode)
                .Select(book => new { book.LIB_Codigo, book.LIB_Activo })
                .FirstOrDefaultAsync(cancellationToken);

            if (byBarcode is not null)
            {
                return $"El código de barras {input.Barcode} ya está registrado en el producto " +
                       $"{byBarcode.LIB_Codigo}" + (byBarcode.LIB_Activo ? "." : " (inactivo).");
            }
        }

        return null;
    }

    private async Task<string> GenerateCodeAsync(
        CancellationToken cancellationToken)
    {
        int next =
            (await _context.Libros.MaxAsync(
                book => (int?)book.LIB_Libro,
                cancellationToken) ?? 0) + 1;

        while (true)
        {
            string code = $"{CodePrefix}{next:D6}";

            if (!await _context.Libros.AnyAsync(
                    book => book.LIB_Codigo == code,
                    cancellationToken))
            {
                return code;
            }

            next++;
        }
    }

    private static void ApplyBookData(
        PB_LIBRO book,
        ProductInput input)
    {
        book.LIB_ISBN10 = input.Isbn10;
        book.LIB_ISBN13 = input.Isbn13;
        book.LIB_CodigoBarras = input.Barcode;
        book.LIB_Titulo = input.Title;
        book.LIB_Subtitulo = input.Subtitle;
        book.LIB_EditorialId = input.EditorialId;
        book.LIB_IdiomaId = input.LanguageId;
        book.LIB_FormatoLibroId = input.FormatId;
        book.LIB_ImpuestoId = input.TaxId;
        book.LIB_NumeroEdicion = input.Edition;
        book.LIB_AnioPublicacion = input.PublicationYear;
        book.LIB_NumeroPaginas = input.PageCount;
        book.LIB_Descripcion = input.Description;
        book.LIB_PermiteVenta = input.AllowsSale;

        if (input.ReferenceCost.HasValue)
        {
            book.LIB_CostoReferencia = input.ReferenceCost.Value;
        }
    }

    private static string AuthorName(
        string? pseudonym,
        string firstName,
        string lastName)
    {
        return string.IsNullOrWhiteSpace(pseudonym)
            ? $"{firstName} {lastName}".Trim()
            : pseudonym;
    }

    private static OperationResult<ProductResponse> ConcurrentConflict()
    {
        return OperationResult<ProductResponse>.Conflict(
            "Otro producto con el mismo código, ISBN o código de barras se guardó " +
            "al mismo tiempo. Revise los datos e intente nuevamente.");
    }

    /// <summary>
    /// Valores del request ya normalizados; los vacíos quedan en null.
    /// </summary>
    private sealed record ProductInput(
        string? Isbn10,
        string? Isbn13,
        string? Barcode,
        string Title,
        string? Subtitle,
        int? EditorialId,
        int LanguageId,
        int FormatId,
        int TaxId,
        string? Edition,
        int? PublicationYear,
        int? PageCount,
        string? Description,
        bool AllowsSale,
        List<int> AuthorIds,
        List<int> CategoryIds,
        int? PrimaryCategoryId,
        IReadOnlyCollection<ProductPriceRequest>? Prices,
        decimal? ReferenceCost)
    {
        public static ProductInput From(SaveProductRequest request)
        {
            List<int> categoryIds = request.CategoryIds.Distinct().ToList();

            return new ProductInput(
                Clean(request.Isbn10) is { } isbn10 ? IsbnRules.Normalize(isbn10) : null,
                Clean(request.Isbn13) is { } isbn13 ? IsbnRules.Normalize(isbn13) : null,
                Clean(request.Barcode),
                request.Title.Trim(),
                Clean(request.Subtitle),
                request.EditorialId,
                request.LanguageId!.Value,
                request.FormatId!.Value,
                request.TaxId!.Value,
                Clean(request.Edition),
                request.PublicationYear,
                request.PageCount,
                Clean(request.Description),
                request.AllowsSale,
                request.AuthorIds.Distinct().ToList(),
                categoryIds,
                request.PrimaryCategoryId ??
                    (categoryIds.Count > 0 ? categoryIds[0] : null),
                request.Prices,
                request.ReferenceCost);
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
