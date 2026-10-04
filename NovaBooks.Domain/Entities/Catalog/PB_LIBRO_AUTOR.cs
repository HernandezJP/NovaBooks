using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_LIBRO_AUTOR
    {
        public int LAU_LibroId { get; set; }

        public int LAU_AutorId { get; set; }

        public int LAU_Orden { get; set; } = 1;

        public string? LAU_TipoParticipacion { get; set; }

        public PB_LIBRO Libro { get; set; } = null!;

        public PB_AUTOR Autor { get; set; } = null!;
    }
}
