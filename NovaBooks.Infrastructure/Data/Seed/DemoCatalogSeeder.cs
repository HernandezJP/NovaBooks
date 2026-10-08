using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Catalog;

namespace NovaBooks.Infrastructure.Data.Seed;

/// <summary>
/// Datos de demostración del catálogo (editoriales, autores y libros) para
/// desarrollo. Editoriales, autores y libros se agregan solo si no existen
/// (por nombre o título), así que puede ejecutarse en cada arranque. Los ISBN son ficticios con
/// formato y dígito de control válidos (prefijo de Guatemala 978-99922).
/// </summary>
public static class DemoCatalogSeeder
{
    private sealed record DemoAuthor(string FirstName, string LastName, string? Pseudonym = null);

    private sealed record DemoBook(
        string Title,
        string? Subtitle,
        string[] Authors,
        string Editorial,
        string Category,
        string Format,
        decimal Price,
        decimal Cost,
        int Year,
        int Pages,
        bool Active = true);

    private static readonly string[] Editorials =
    [
        "Debolsillo", "Salamandra", "Paidós", "Alfaguara", "Planeta",
        "Océano", "Anagrama", "Penguin Random House", "Santillana", "Norma"
    ];

    private static readonly Dictionary<string, DemoAuthor> Authors = new()
    {
        ["garcia-marquez"] = new("Gabriel", "García Márquez"),
        ["saint-exupery"] = new("Antoine", "de Saint-Exupéry"),
        ["clear"] = new("James", "Clear"),
        ["harari"] = new("Yuval Noah", "Harari"),
        ["michaelides"] = new("Alex", "Michaelides"),
        ["allende"] = new("Isabel", "Allende"),
        ["ruiz-zafon"] = new("Carlos", "Ruiz Zafón"),
        ["cervantes"] = new("Miguel", "de Cervantes"),
        ["orwell"] = new("Eric Arthur", "Blair", "George Orwell"),
        ["herbert"] = new("Frank", "Herbert"),
        ["vargas-llosa"] = new("Mario", "Vargas Llosa"),
        ["cortazar"] = new("Julio", "Cortázar"),
        ["coelho"] = new("Paulo", "Coelho"),
        ["borges"] = new("Jorge Luis", "Borges"),
        ["asturias"] = new("Miguel Ángel", "Asturias"),
        ["hawking"] = new("Stephen", "Hawking"),
        ["rowling"] = new("Joanne", "Rowling", "J. K. Rowling"),
        ["sun-tzu"] = new("Sun", "Tzu", "Sun Tzu")
    };

    private static readonly DemoBook[] Books =
    [
        new("Cien años de soledad", null, ["garcia-marquez"], "Debolsillo", "LITERATURA", "BOLSILLO", 145m, 92m, 2015, 496),
        new("El amor en los tiempos del cólera", null, ["garcia-marquez"], "Debolsillo", "LITERATURA", "BOLSILLO", 135m, 85m, 2014, 464),
        new("El principito", null, ["saint-exupery"], "Salamandra", "INFANTIL", "TAPA_DURA", 95m, 55m, 2019, 96),
        new("Hábitos atómicos", "Cambios pequeños, resultados extraordinarios", ["clear"], "Paidós", "AUTOAYUDA", "TAPA_BLANDA", 160m, 98m, 2020, 328),
        new("Sapiens", "De animales a dioses", ["harari"], "Debolsillo", "NO_FICCION", "BOLSILLO", 180m, 110m, 2015, 496),
        new("Homo Deus", "Breve historia del mañana", ["harari"], "Debolsillo", "NO_FICCION", "BOLSILLO", 180m, 108m, 2017, 496),
        new("La paciente silenciosa", null, ["michaelides"], "Alfaguara", "LITERATURA", "TAPA_BLANDA", 175m, 105m, 2019, 352, Active: false),
        new("La casa de los espíritus", null, ["allende"], "Debolsillo", "LITERATURA", "BOLSILLO", 140m, 84m, 2016, 512),
        new("La sombra del viento", null, ["ruiz-zafon"], "Planeta", "LITERATURA", "TAPA_BLANDA", 165m, 99m, 2016, 576),
        new("Don Quijote de la Mancha", null, ["cervantes"], "Alfaguara", "LITERATURA", "TAPA_DURA", 250m, 150m, 2015, 1376),
        new("1984", null, ["orwell"], "Debolsillo", "LITERATURA", "BOLSILLO", 120m, 70m, 2013, 352),
        new("Dune", null, ["herbert"], "Debolsillo", "LITERATURA", "BOLSILLO", 190m, 115m, 2021, 784),
        new("La ciudad y los perros", null, ["vargas-llosa"], "Alfaguara", "LITERATURA", "TAPA_BLANDA", 155m, 93m, 2012, 448),
        new("Rayuela", null, ["cortazar"], "Alfaguara", "LITERATURA", "TAPA_BLANDA", 170m, 102m, 2019, 736),
        new("El alquimista", null, ["coelho"], "Planeta", "LITERATURA", "BOLSILLO", 110m, 64m, 2016, 192),
        new("Ficciones", null, ["borges"], "Debolsillo", "LITERATURA", "BOLSILLO", 125m, 75m, 2011, 224),
        new("El Señor Presidente", null, ["asturias"], "Océano", "LITERATURA", "TAPA_BLANDA", 150m, 90m, 2018, 352),
        new("Breve historia del tiempo", "Del big bang a los agujeros negros", ["hawking"], "Planeta", "CIENCIA", "TAPA_BLANDA", 165m, 99m, 2015, 256),
        new("Harry Potter y la piedra filosofal", null, ["rowling"], "Salamandra", "INFANTIL", "TAPA_BLANDA", 185m, 112m, 2020, 256),
        new("El arte de la guerra", null, ["sun-tzu"], "Paidós", "NO_FICCION", "BOLSILLO", 85m, 45m, 2018, 160)
    ];

    public static async Task SeedAsync(
        AppDbContext context,
        string imagesDirectory,
        CancellationToken cancellationToken = default)
    {
        await SeedCatalogAsync(context, cancellationToken);
        await SeedCoversAsync(context, imagesDirectory, cancellationToken);
    }

    private static async Task SeedCatalogAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        // ---------- Editoriales ----------
        HashSet<string> existingEditorials =
            (await context.Editoriales.Select(item => item.EDI_Nombre).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int nextEditorial = (await context.Editoriales.MaxAsync(item => (int?)item.EDI_Editorial, cancellationToken) ?? 0) + 1;

        foreach (string name in Editorials.Where(name => !existingEditorials.Contains(name)))
        {
            context.Editoriales.Add(new PB_EDITORIAL
            {
                EDI_Codigo = $"EDI-{nextEditorial++:D4}",
                EDI_Nombre = name,
                EDI_Activo = true,
                EDI_FechaCreacion = now
            });
        }

        // ---------- Autores ----------
        var existingAuthors =
            await context.Autores
                .Select(item => new { item.AUT_PrimerNombre, item.AUT_PrimerApellido })
                .ToListAsync(cancellationToken);

        foreach (DemoAuthor author in Authors.Values.Where(author => !existingAuthors.Any(item =>
                     string.Equals(item.AUT_PrimerNombre, author.FirstName, StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(item.AUT_PrimerApellido, author.LastName, StringComparison.OrdinalIgnoreCase))))
        {
            context.Autores.Add(new PB_AUTOR
            {
                AUT_PrimerNombre = author.FirstName,
                AUT_PrimerApellido = author.LastName,
                AUT_Seudonimo = author.Pseudonym,
                AUT_Activo = true,
                AUT_FechaCreacion = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        // ---------- Libros (solo los que no existan por título) ----------
        var existingBooks = await context.Libros
            .Select(item => new { item.LIB_Titulo, item.LIB_Codigo, item.LIB_ISBN13 })
            .ToListAsync(cancellationToken);

        HashSet<string> existingTitles = existingBooks
            .Select(item => item.LIB_Titulo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> usedCodes = existingBooks
            .Select(item => item.LIB_Codigo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> usedIsbns = existingBooks
            .Where(item => item.LIB_ISBN13 != null)
            .Select(item => item.LIB_ISBN13!)
            .ToHashSet();

        DemoBook[] pendingBooks = Books
            .Where(book => !existingTitles.Contains(book.Title))
            .ToArray();

        if (pendingBooks.Length == 0)
        {
            return;
        }

        Dictionary<string, int> editorialIds = await context.Editoriales
            .ToDictionaryAsync(item => item.EDI_Nombre, item => item.EDI_Editorial, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var authorRows = await context.Autores
            .Select(item => new { item.AUT_Autor, item.AUT_PrimerNombre, item.AUT_PrimerApellido })
            .ToListAsync(cancellationToken);

        Dictionary<string, int> categoryIds = await context.Categorias
            .ToDictionaryAsync(item => item.CAT_Codigo, item => item.CAT_Categoria, cancellationToken);

        Dictionary<string, int> formatIds = await context.FormatosLibro
            .ToDictionaryAsync(item => item.FLI_Codigo, item => item.FLI_FormatoLibro, cancellationToken);

        int? spanishId = await context.Idiomas
            .Where(item => item.IDI_Codigo == "ES")
            .Select(item => (int?)item.IDI_Idioma)
            .FirstOrDefaultAsync(cancellationToken);

        int? taxId = await context.Impuestos
            .OrderBy(item => item.IMP_Impuesto)
            .Select(item => (int?)item.IMP_Impuesto)
            .FirstOrDefaultAsync(cancellationToken);

        var priceLists = await context.ListasPrecio
            .Select(item => new { item.LPR_ListaPrecio, item.LPR_EsPredeterminada })
            .ToListAsync(cancellationToken);

        if (spanishId is null || taxId is null || priceLists.Count == 0)
        {
            return;
        }

        int codeNumber = (await context.Libros.MaxAsync(item => (int?)item.LIB_Libro, cancellationToken) ?? 0) + 1;
        int isbnNumber = 1;

        foreach (DemoBook demo in pendingBooks)
        {
            string code;
            do
            {
                code = $"LIB-{codeNumber++:D6}";
            }
            while (!usedCodes.Add(code));

            string isbn;
            do
            {
                isbn = BuildIsbn13(isbnNumber++);
            }
            while (!usedIsbns.Add(isbn));

            PB_LIBRO book = new()
            {
                LIB_Codigo = code,
                LIB_ISBN13 = isbn,
                LIB_Titulo = demo.Title,
                LIB_Subtitulo = demo.Subtitle,
                LIB_EditorialId = editorialIds.GetValueOrDefault(demo.Editorial),
                LIB_IdiomaId = spanishId.Value,
                LIB_FormatoLibroId = formatIds[demo.Format],
                LIB_ImpuestoId = taxId.Value,
                LIB_AnioPublicacion = demo.Year,
                LIB_NumeroPaginas = demo.Pages,
                LIB_CostoReferencia = demo.Cost,
                LIB_PermiteVenta = true,
                LIB_Activo = demo.Active,
                LIB_FechaCreacion = now
            };

            for (int order = 0; order < demo.Authors.Length; order++)
            {
                DemoAuthor author = Authors[demo.Authors[order]];

                int authorId = authorRows.First(item =>
                    string.Equals(item.AUT_PrimerNombre, author.FirstName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.AUT_PrimerApellido, author.LastName, StringComparison.OrdinalIgnoreCase)).AUT_Autor;

                book.LibrosAutores.Add(new PB_LIBRO_AUTOR { LAU_AutorId = authorId, LAU_Orden = order + 1 });
            }

            if (categoryIds.TryGetValue(demo.Category, out int categoryId))
            {
                book.LibrosCategorias.Add(new PB_LIBRO_CATEGORIA { LCA_CategoriaId = categoryId, LCA_Principal = true });
            }

            foreach (var list in priceLists)
            {
                // Lista mayorista con 15 % de descuento sobre el precio general.
                decimal price = list.LPR_EsPredeterminada
                    ? demo.Price
                    : Math.Round(demo.Price * 0.85m, 2);

                book.Precios.Add(new PB_LIBRO_PRECIO
                {
                    LIP_ListaPrecioId = list.LPR_ListaPrecio,
                    LIP_Precio = price,
                    LIP_FechaInicio = now,
                    LIP_Activo = true,
                    LIP_FechaCreacion = now
                });
            }

            context.Libros.Add(book);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Asigna una portada ilustrada (Data/Seed/Covers) a los libros de
    /// demostración que todavía no tienen imagen.
    /// </summary>
    private static async Task SeedCoversAsync(
        AppDbContext context,
        string imagesDirectory,
        CancellationToken cancellationToken)
    {
        string coversDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "Covers");

        if (!Directory.Exists(coversDirectory))
        {
            return;
        }

        HashSet<string> demoTitles = Books
            .Select(book => book.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<PB_LIBRO> books = await context.Libros
            .Where(book => book.LIB_RutaImagen == null)
            .ToListAsync(cancellationToken);

        books = books.Where(book => demoTitles.Contains(book.LIB_Titulo)).ToList();

        if (books.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(imagesDirectory);

        foreach (PB_LIBRO book in books)
        {
            string source = Path.Combine(coversDirectory, Slug(book.LIB_Titulo) + ".png");

            if (!File.Exists(source))
            {
                continue;
            }

            string fileName = $"{book.LIB_Libro}-{Guid.NewGuid():N}.png";
            File.Copy(source, Path.Combine(imagesDirectory, fileName));
            book.LIB_RutaImagen = fileName;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>"Cien años de soledad" → "cien-anos-de-soledad".</summary>
    private static string Slug(string title)
    {
        string normalized = title.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new();

        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// ISBN-13 ficticio con dígito de control válido: 978-99922-NNNN-C.
    /// </summary>
    private static string BuildIsbn13(int sequence)
    {
        string body = $"97899922{sequence:D4}";
        int sum = 0;

        for (int index = 0; index < 12; index++)
        {
            int digit = body[index] - '0';
            sum += index % 2 == 0 ? digit : digit * 3;
        }

        return body + ((10 - sum % 10) % 10);
    }
}
