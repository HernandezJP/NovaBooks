using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_LIBRO_CATEGORIA
    {
        public int LCA_LibroId { get; set; }

        public int LCA_CategoriaId { get; set; }

        public bool LCA_Principal { get; set; }

        public PB_LIBRO Libro { get; set; } = null!;

        public PB_CATEGORIA Categoria { get; set; } = null!;
    }
}
