using NovaBooks.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_MOVIMIENTO_INVENTARIO_DETALLE
    {
        public int MDE_MovimientoInventarioDetalle { get; set; }

        public int MDE_MovimientoInventarioId { get; set; }

        public int MDE_LibroId { get; set; }

        public int? MDE_LoteId { get; set; }

        public decimal MDE_Cantidad { get; set; }

        public decimal MDE_CostoUnitario { get; set; }

        public decimal MDE_CostoTotal { get; set; }

        public string? MDE_Observaciones { get; set; }

        public PB_MOVIMIENTO_INVENTARIO MovimientoInventario { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;

        public PB_LOTE? Lote { get; set; }
    }
}
