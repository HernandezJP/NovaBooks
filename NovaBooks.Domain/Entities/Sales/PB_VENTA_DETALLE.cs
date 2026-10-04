using NovaBooks.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_VENTA_DETALLE
    {
        public int VDE_VentaDetalle { get; set; }

        public int VDE_VentaId { get; set; }

        public int VDE_LibroId { get; set; }

        public int VDE_NumeroLinea { get; set; }

        public decimal VDE_Cantidad { get; set; }

        public decimal VDE_PrecioUnitario { get; set; }

        public decimal VDE_CostoUnitario { get; set; }

        public decimal VDE_PorcentajeDescuento { get; set; }

        public decimal VDE_MontoDescuento { get; set; }

        public decimal VDE_PorcentajeImpuesto { get; set; }

        public decimal VDE_MontoImpuesto { get; set; }

        public decimal VDE_Subtotal { get; set; }

        public decimal VDE_Total { get; set; }

        public decimal VDE_CantidadDevuelta { get; set; }

        public PB_VENTA Venta { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;

        public ICollection<PB_DEVOLUCION_VENTA_DETALLE> DevolucionesDetalles { get; set; } = [];
    }
}
