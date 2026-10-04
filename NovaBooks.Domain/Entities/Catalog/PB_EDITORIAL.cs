using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_EDITORIAL
    {
        public int EDI_Editorial { get; set; }

        public string EDI_Codigo { get; set; } = string.Empty;

        public string EDI_Nombre { get; set; } = string.Empty;

        public string? EDI_SitioWeb { get; set; }

        public string? EDI_Correo { get; set; }

        public string? EDI_Telefono { get; set; }

        public bool EDI_Activo { get; set; } = true;

        public DateTime EDI_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? EDI_FechaModificacion { get; set; }

        public ICollection<PB_LIBRO> Libros { get; set; } = [];
    }
}
