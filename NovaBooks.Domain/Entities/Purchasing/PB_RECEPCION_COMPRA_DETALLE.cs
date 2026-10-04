using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Domain.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Purchasing
{
    public class PB_RECEPCION_COMPRA_DETALLE
    {
        public int RCD_RecepcionCompraDetalle { get; set; }

        public int RCD_RecepcionCompraId { get; set; }

        public int RCD_OrdenCompraDetalleId { get; set; }

        public int RCD_LibroId { get; set; }

        public int? RCD_LoteId { get; set; }

        public decimal RCD_CantidadRecibida { get; set; }

        public decimal RCD_CantidadAceptada { get; set; }

        public decimal RCD_CantidadRechazada { get; set; }

        public decimal RCD_CostoUnitario { get; set; }

        public string? RCD_MotivoRechazo { get; set; }

        public string? RCD_Observaciones { get; set; }

        public PB_RECEPCION_COMPRA RecepcionCompra { get; set; } = null!;

        public PB_ORDEN_COMPRA_DETALLE OrdenCompraDetalle { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;

        public PB_LOTE? Lote { get; set; }
    }
}
