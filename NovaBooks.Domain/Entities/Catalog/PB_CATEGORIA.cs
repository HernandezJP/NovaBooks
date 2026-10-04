using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_CATEGORIA
    {
        public int CAT_Categoria { get; set; }

        public int? CAT_CategoriaPadreId { get; set; }

        public string CAT_Codigo { get; set; } = string.Empty;

        public string CAT_Nombre { get; set; } = string.Empty;

        public string? CAT_Descripcion { get; set; }

        public bool CAT_Activo { get; set; } = true;

        public DateTime CAT_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? CAT_FechaModificacion { get; set; }

        public PB_CATEGORIA? CategoriaPadre { get; set; }

        public ICollection<PB_CATEGORIA> Subcategorias { get; set; } = [];

        public ICollection<PB_LIBRO_CATEGORIA> LibrosCategorias { get; set; } = [];
    }
}
