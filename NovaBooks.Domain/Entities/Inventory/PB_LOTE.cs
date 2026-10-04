using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Domain.Entities.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_LOTE
    {
        public int LOT_Lote { get; set; }

        public int LOT_AlmacenId { get; set; }

        public int LOT_LibroId { get; set; }

        public int? LOT_ProveedorId { get; set; }

        public string LOT_Numero { get; set; } = string.Empty;

        public DateOnly LOT_FechaIngreso { get; set; }

        public DateOnly? LOT_FechaVencimiento { get; set; }

        public decimal LOT_CantidadInicial { get; set; }

        public decimal LOT_CantidadActual { get; set; }

        public decimal LOT_CostoUnitario { get; set; }

        public bool LOT_Activo { get; set; } = true;

        public DateTime LOT_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? LOT_FechaModificacion { get; set; }

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;

        public PB_PROVEEDOR? Proveedor { get; set; }

        public ICollection<PB_MOVIMIENTO_INVENTARIO_DETALLE> MovimientosDetalles { get; set; } = [];
    }
}
