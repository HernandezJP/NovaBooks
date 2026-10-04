using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_IDIOMA
    {
        public int IDI_Idioma { get; set; }

        public string IDI_Codigo { get; set; } = string.Empty;

        public string IDI_Nombre { get; set; } = string.Empty;

        public bool IDI_Activo { get; set; } = true;

        public ICollection<PB_LIBRO> Libros { get; set; } = [];
    }
}
