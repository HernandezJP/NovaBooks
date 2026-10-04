using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_IMPUESTO
    {
        public int IMP_Impuesto { get; set; }

        public string IMP_Codigo { get; set; } = string.Empty;

        public string IMP_Nombre { get; set; } = string.Empty;

        public decimal IMP_Porcentaje { get; set; }

        public DateOnly IMP_FechaInicio { get; set; }

        public DateOnly? IMP_FechaFin { get; set; }

        public bool IMP_Activo { get; set; } = true;

        public DateTime IMP_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? IMP_FechaModificacion { get; set; }

        public ICollection<PB_LIBRO> Libros { get; set; } = [];
    }
}
