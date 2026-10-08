using Microsoft.EntityFrameworkCore;
using NovaBooks.Domain.Entities.Catalog;

namespace NovaBooks.Infrastructure.Data.Configurations;

public static class CatalogConfiguration
{
    public static void ConfigureCatalog(this ModelBuilder modelBuilder)
    {
        ConfigureAutor(modelBuilder);
        ConfigureEditorial(modelBuilder);
        ConfigureCategoria(modelBuilder);
        ConfigureIdioma(modelBuilder);
        ConfigureFormatoLibro(modelBuilder);
        ConfigureImpuesto(modelBuilder);
        ConfigureLibro(modelBuilder);
        ConfigureLibroAutor(modelBuilder);
        ConfigureLibroCategoria(modelBuilder);
        ConfigureListaPrecio(modelBuilder);
        ConfigureLibroPrecio(modelBuilder);
    }

    private static void ConfigureAutor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_AUTOR>(builder =>
        {
            builder.ToTable("PB_AUTOR");

            builder.HasKey(x => x.AUT_Autor);

            builder.Property(x => x.AUT_PrimerNombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.AUT_SegundoNombre)
                .HasMaxLength(100);

            builder.Property(x => x.AUT_PrimerApellido)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.AUT_SegundoApellido)
                .HasMaxLength(100);

            builder.Property(x => x.AUT_Seudonimo)
                .HasMaxLength(150);

            builder.Property(x => x.AUT_FechaNacimiento)
                .HasColumnType("date");

            builder.Property(x => x.AUT_FechaFallecimiento)
                .HasColumnType("date");

            builder.Property(x => x.AUT_Biografia)
                .HasMaxLength(4000);

            builder.Property(x => x.AUT_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.AUT_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => new
            {
                x.AUT_PrimerNombre,
                x.AUT_PrimerApellido
            });

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_AUTOR_FECHAS",
                    "[AUT_FechaFallecimiento] IS NULL OR " +
                    "[AUT_FechaNacimiento] IS NULL OR " +
                    "[AUT_FechaFallecimiento] >= " +
                    "[AUT_FechaNacimiento]");
            });
        });
    }

    private static void ConfigureEditorial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_EDITORIAL>(builder =>
        {
            builder.ToTable("PB_EDITORIAL");

            builder.HasKey(x => x.EDI_Editorial);

            builder.Property(x => x.EDI_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.EDI_Nombre)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.EDI_SitioWeb)
                .HasMaxLength(500);

            builder.Property(x => x.EDI_Correo)
                .HasMaxLength(256);

            builder.Property(x => x.EDI_Telefono)
                .HasMaxLength(25)
                .IsUnicode(false);

            builder.Property(x => x.EDI_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.EDI_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.EDI_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.EDI_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureCategoria(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_CATEGORIA>(builder =>
        {
            builder.ToTable("PB_CATEGORIA");

            builder.HasKey(x => x.CAT_Categoria);

            builder.Property(x => x.CAT_Codigo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.CAT_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.CAT_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.CAT_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.CAT_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.CAT_Codigo)
                .IsUnique();

            builder.HasIndex(x => new
            {
                x.CAT_CategoriaPadreId,
                x.CAT_Nombre
            }).IsUnique();

            builder.HasOne(x => x.CategoriaPadre)
                .WithMany(x => x.Subcategorias)
                .HasForeignKey(x => x.CAT_CategoriaPadreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureIdioma(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_IDIOMA>(builder =>
        {
            builder.ToTable("PB_IDIOMA");

            builder.HasKey(x => x.IDI_Idioma);

            builder.Property(x => x.IDI_Codigo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.IDI_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IDI_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.IDI_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.IDI_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureFormatoLibro(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_FORMATO_LIBRO>(builder =>
        {
            builder.ToTable("PB_FORMATO_LIBRO");

            builder.HasKey(x => x.FLI_FormatoLibro);

            builder.Property(x => x.FLI_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.FLI_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.FLI_Descripcion)
                .HasMaxLength(250);

            builder.Property(x => x.FLI_Activo)
                .HasDefaultValue(true);

            builder.HasIndex(x => x.FLI_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.FLI_Nombre)
                .IsUnique();
        });
    }

    private static void ConfigureImpuesto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_IMPUESTO>(builder =>
        {
            builder.ToTable("PB_IMPUESTO");

            builder.HasKey(x => x.IMP_Impuesto);

            builder.Property(x => x.IMP_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.IMP_Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IMP_Porcentaje)
                .HasPrecision(5, 2);

            builder.Property(x => x.IMP_FechaInicio)
                .HasColumnType("date");

            builder.Property(x => x.IMP_FechaFin)
                .HasColumnType("date");

            builder.Property(x => x.IMP_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.IMP_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.IMP_Codigo)
                .IsUnique();

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_IMPUESTO_PORCENTAJE",
                    "[IMP_Porcentaje] >= 0 AND " +
                    "[IMP_Porcentaje] <= 100");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_IMPUESTO_FECHAS",
                    "[IMP_FechaFin] IS NULL OR " +
                    "[IMP_FechaFin] >= [IMP_FechaInicio]");
            });
        });
    }

    private static void ConfigureLibro(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_LIBRO>(builder =>
        {
            builder.ToTable("PB_LIBRO");

            builder.HasKey(x => x.LIB_Libro);

            builder.Property(x => x.LIB_Codigo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.LIB_ISBN10)
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.Property(x => x.LIB_ISBN13)
                .HasMaxLength(13)
                .IsUnicode(false);

            builder.Property(x => x.LIB_CodigoBarras)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.Property(x => x.LIB_Titulo)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.LIB_Subtitulo)
                .HasMaxLength(250);

            builder.Property(x => x.LIB_NumeroEdicion)
                .HasMaxLength(50);

            builder.Property(x => x.LIB_AltoCentimetros)
                .HasPrecision(10, 2);

            builder.Property(x => x.LIB_AnchoCentimetros)
                .HasPrecision(10, 2);

            builder.Property(x => x.LIB_GrosorCentimetros)
                .HasPrecision(10, 2);

            builder.Property(x => x.LIB_PesoGramos)
                .HasPrecision(10, 2);

            builder.Property(x => x.LIB_Descripcion)
                .HasMaxLength(4000);

            builder.Property(x => x.LIB_RutaImagen)
                .HasMaxLength(1000);

            builder.Property(x => x.LIB_CostoReferencia)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(x => x.LIB_PermiteVenta)
                .HasDefaultValue(true);

            builder.Property(x => x.LIB_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.LIB_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.LIB_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.LIB_ISBN10)
                .IsUnique()
                .HasFilter("[LIB_ISBN10] IS NOT NULL");

            builder.HasIndex(x => x.LIB_ISBN13)
                .IsUnique()
                .HasFilter("[LIB_ISBN13] IS NOT NULL");

            builder.HasIndex(x => x.LIB_CodigoBarras)
                .IsUnique()
                .HasFilter("[LIB_CodigoBarras] IS NOT NULL");

            builder.HasIndex(x => x.LIB_Titulo);

            builder.HasIndex(x => x.LIB_EditorialId);

            builder.HasIndex(x => x.LIB_IdiomaId);

            builder.HasIndex(x => x.LIB_FormatoLibroId);

            builder.HasIndex(x => x.LIB_ImpuestoId);

            builder.HasOne(x => x.Editorial)
                .WithMany(x => x.Libros)
                .HasForeignKey(x => x.LIB_EditorialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Idioma)
                .WithMany(x => x.Libros)
                .HasForeignKey(x => x.LIB_IdiomaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.FormatoLibro)
                .WithMany(x => x.Libros)
                .HasForeignKey(x => x.LIB_FormatoLibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Impuesto)
                .WithMany(x => x.Libros)
                .HasForeignKey(x => x.LIB_ImpuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_LIBRO_ANIO_PUBLICACION",
                    "[LIB_AnioPublicacion] IS NULL OR " +
                    "[LIB_AnioPublicacion] BETWEEN 1000 AND 9999");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_LIBRO_NUMERO_PAGINAS",
                    "[LIB_NumeroPaginas] IS NULL OR " +
                    "[LIB_NumeroPaginas] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_LIBRO_COSTO_REFERENCIA",
                    "[LIB_CostoReferencia] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_LIBRO_DIMENSIONES",
                    "([LIB_AltoCentimetros] IS NULL OR " +
                    "[LIB_AltoCentimetros] > 0) AND " +
                    "([LIB_AnchoCentimetros] IS NULL OR " +
                    "[LIB_AnchoCentimetros] > 0) AND " +
                    "([LIB_GrosorCentimetros] IS NULL OR " +
                    "[LIB_GrosorCentimetros] > 0) AND " +
                    "([LIB_PesoGramos] IS NULL OR " +
                    "[LIB_PesoGramos] > 0)");
            });
        });
    }

    private static void ConfigureLibroAutor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_LIBRO_AUTOR>(builder =>
        {
            builder.ToTable("PB_LIBRO_AUTOR");

            builder.HasKey(x => new
            {
                x.LAU_LibroId,
                x.LAU_AutorId
            });

            builder.Property(x => x.LAU_Orden)
                .HasDefaultValue(1);

            builder.Property(x => x.LAU_TipoParticipacion)
                .HasMaxLength(50);

            builder.HasOne(x => x.Libro)
                .WithMany(x => x.LibrosAutores)
                .HasForeignKey(x => x.LAU_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Autor)
                .WithMany(x => x.LibrosAutores)
                .HasForeignKey(x => x.LAU_AutorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_LIBRO_AUTOR_ORDEN",
                    "[LAU_Orden] > 0");
            });
        });
    }

    private static void ConfigureLibroCategoria(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_LIBRO_CATEGORIA>(builder =>
        {
            builder.ToTable("PB_LIBRO_CATEGORIA");

            builder.HasKey(x => new
            {
                x.LCA_LibroId,
                x.LCA_CategoriaId
            });

            builder.Property(x => x.LCA_Principal)
                .HasDefaultValue(false);

            builder.HasIndex(x => new
            {
                x.LCA_LibroId,
                x.LCA_Principal
            })
            .IsUnique()
            .HasFilter("[LCA_Principal] = 1");

            builder.HasOne(x => x.Libro)
                .WithMany(x => x.LibrosCategorias)
                .HasForeignKey(x => x.LCA_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Categoria)
                .WithMany(x => x.LibrosCategorias)
                .HasForeignKey(x => x.LCA_CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureListaPrecio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_LISTA_PRECIO>(builder =>
        {
            builder.ToTable("PB_LISTA_PRECIO");

            builder.HasKey(x => x.LPR_ListaPrecio);

            builder.Property(x => x.LPR_Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.LPR_Nombre)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.LPR_Descripcion)
                .HasMaxLength(500);

            builder.Property(x => x.LPR_EsPredeterminada)
                .HasDefaultValue(false);

            builder.Property(x => x.LPR_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.LPR_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.LPR_Codigo)
                .IsUnique();

            builder.HasIndex(x => x.LPR_Nombre)
                .IsUnique();

            builder.HasIndex(x => x.LPR_EsPredeterminada)
                .IsUnique()
                .HasFilter(
                    "[LPR_EsPredeterminada] = 1 AND " +
                    "[LPR_Activo] = 1");
        });
    }

    private static void ConfigureLibroPrecio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PB_LIBRO_PRECIO>(builder =>
        {
            builder.ToTable("PB_LIBRO_PRECIO");

            builder.HasKey(x => x.LIP_LibroPrecio);

            builder.Property(x => x.LIP_Precio)
                .HasPrecision(18, 2);

            builder.Property(x => x.LIP_FechaInicio)
                .HasColumnType("datetime2");

            builder.Property(x => x.LIP_FechaFin)
                .HasColumnType("datetime2");

            builder.Property(x => x.LIP_Activo)
                .HasDefaultValue(true);

            builder.Property(x => x.LIP_FechaCreacion)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => new
            {
                x.LIP_LibroId,
                x.LIP_ListaPrecioId,
                x.LIP_FechaInicio
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.LIP_LibroId,
                x.LIP_ListaPrecioId,
                x.LIP_Activo
            });

            builder.HasOne(x => x.Libro)
                .WithMany(x => x.Precios)
                .HasForeignKey(x => x.LIP_LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ListaPrecio)
                .WithMany(x => x.Precios)
                .HasForeignKey(x => x.LIP_ListaPrecioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PB_LIBRO_PRECIO_PRECIO",
                    "[LIP_Precio] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_PB_LIBRO_PRECIO_FECHAS",
                    "[LIP_FechaFin] IS NULL OR " +
                    "[LIP_FechaFin] >= [LIP_FechaInicio]");
            });
        });
    }
}