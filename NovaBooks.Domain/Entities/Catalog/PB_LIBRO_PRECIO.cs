using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Catalog
{
    public class PB_LIBRO_PRECIO
    {
        public int LIP_LibroPrecio { get; set; }

        public int LIP_LibroId { get; set; }

        public int LIP_ListaPrecioId { get; set; }

        public decimal LIP_Precio { get; set; }

        public DateTime LIP_FechaInicio { get; set; }

        public DateTime? LIP_FechaFin { get; set; }

        public bool LIP_Activo { get; set; } = true;

        public DateTime LIP_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_LIBRO Libro { get; set; } = null!;

        public PB_LISTA_PRECIO ListaPrecio { get; set; } = null!;
    }
}
