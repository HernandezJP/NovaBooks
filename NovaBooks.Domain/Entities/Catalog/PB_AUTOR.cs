using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_AUTOR
    {
        public int AUT_Autor { get; set; }

        public string AUT_PrimerNombre { get; set; } = string.Empty;

        public string? AUT_SegundoNombre { get; set; }

        public string AUT_PrimerApellido { get; set; } = string.Empty;

        public string? AUT_SegundoApellido { get; set; }

        public string? AUT_Seudonimo { get; set; }

        public DateOnly? AUT_FechaNacimiento { get; set; }

        public DateOnly? AUT_FechaFallecimiento { get; set; }

        public string? AUT_Biografia { get; set; }

        public bool AUT_Activo { get; set; } = true;

        public DateTime AUT_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? AUT_FechaModificacion { get; set; }

        public ICollection<PB_LIBRO_AUTOR> LibrosAutores { get; set; } = [];
    }
}
