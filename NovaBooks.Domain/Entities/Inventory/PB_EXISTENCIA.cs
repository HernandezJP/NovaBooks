using NovaBooks.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_EXISTENCIA
    {
        public int EXI_Existencia { get; set; }

        public int EXI_AlmacenId { get; set; }

        public int EXI_LibroId { get; set; }

        public decimal EXI_CantidadDisponible { get; set; }

        public decimal EXI_CantidadReservada { get; set; }

        public decimal EXI_StockMinimo { get; set; }

        public decimal EXI_StockMaximo { get; set; }

        public string? EXI_Ubicacion { get; set; }

        public DateTime? EXI_FechaUltimoMovimiento { get; set; }

        public DateTime EXI_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? EXI_FechaModificacion { get; set; }

        public byte[] EXI_Version { get; set; } = [];

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;
    }
}
