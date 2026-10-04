using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_LIBRO
    {
        public int LIB_Libro { get; set; }

        public string LIB_Codigo { get; set; } = string.Empty;

        public string? LIB_ISBN10 { get; set; }

        public string? LIB_ISBN13 { get; set; }

        public string? LIB_CodigoBarras { get; set; }

        public string LIB_Titulo { get; set; } = string.Empty;

        public string? LIB_Subtitulo { get; set; }

        public int? LIB_EditorialId { get; set; }

        public int LIB_IdiomaId { get; set; }

        public int LIB_FormatoLibroId { get; set; }

        public int LIB_ImpuestoId { get; set; }

        public string? LIB_NumeroEdicion { get; set; }

        public int? LIB_AnioPublicacion { get; set; }

        public int? LIB_NumeroPaginas { get; set; }

        public decimal? LIB_AltoCentimetros { get; set; }

        public decimal? LIB_AnchoCentimetros { get; set; }

        public decimal? LIB_GrosorCentimetros { get; set; }

        public decimal? LIB_PesoGramos { get; set; }

        public string? LIB_Descripcion { get; set; }

        public string? LIB_RutaImagen { get; set; }

        public bool LIB_PermiteVenta { get; set; } = true;

        public bool LIB_Activo { get; set; } = true;

        public DateTime LIB_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? LIB_FechaModificacion { get; set; }

        public PB_EDITORIAL? Editorial { get; set; }

        public PB_IDIOMA Idioma { get; set; } = null!;

        public PB_FORMATO_LIBRO FormatoLibro { get; set; } = null!;

        public PB_IMPUESTO Impuesto { get; set; } = null!;

        public ICollection<PB_LIBRO_AUTOR> LibrosAutores { get; set; } = [];

        public ICollection<PB_LIBRO_CATEGORIA> LibrosCategorias { get; set; } = [];

        public ICollection<PB_LIBRO_PRECIO> Precios { get; set; } = [];
    }
}
