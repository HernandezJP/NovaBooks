using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_LISTA_PRECIO
    {
        public int LPR_ListaPrecio { get; set; }

        public string LPR_Codigo { get; set; } = string.Empty;

        public string LPR_Nombre { get; set; } = string.Empty;

        public string? LPR_Descripcion { get; set; }

        public bool LPR_EsPredeterminada { get; set; }

        public bool LPR_Activo { get; set; } = true;

        public DateTime LPR_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? LPR_FechaModificacion { get; set; }

        public ICollection<PB_LIBRO_PRECIO> Precios { get; set; } = [];
    }
}
