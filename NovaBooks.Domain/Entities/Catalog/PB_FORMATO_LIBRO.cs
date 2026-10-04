using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_FORMATO_LIBRO
    {
        public int FLI_FormatoLibro { get; set; }

        public string FLI_Codigo { get; set; } = string.Empty;

        public string FLI_Nombre { get; set; } = string.Empty;

        public string? FLI_Descripcion { get; set; }

        public bool FLI_Activo { get; set; } = true;

        public ICollection<PB_LIBRO> Libros { get; set; } = [];
    }
}
