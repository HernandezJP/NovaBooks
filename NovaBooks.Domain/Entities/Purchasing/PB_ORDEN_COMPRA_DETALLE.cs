using NovaBooks.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Purchasing
{
    public class PB_ORDEN_COMPRA_DETALLE
    {
        public int OCD_OrdenCompraDetalle { get; set; }

        public int OCD_OrdenCompraId { get; set; }

        public int OCD_LibroId { get; set; }

        public decimal OCD_CantidadSolicitada { get; set; }

        public decimal OCD_CantidadRecibida { get; set; }

        public decimal OCD_CostoUnitario { get; set; }

        public decimal OCD_PorcentajeDescuento { get; set; }

        public decimal OCD_MontoDescuento { get; set; }

        public decimal OCD_PorcentajeImpuesto { get; set; }

        public decimal OCD_MontoImpuesto { get; set; }

        public decimal OCD_Subtotal { get; set; }

        public decimal OCD_Total { get; set; }

        public string? OCD_Observaciones { get; set; }

        public PB_ORDEN_COMPRA OrdenCompra { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;

        public ICollection<PB_RECEPCION_COMPRA_DETALLE> RecepcionesDetalles { get; set; } = [];
    }
}
