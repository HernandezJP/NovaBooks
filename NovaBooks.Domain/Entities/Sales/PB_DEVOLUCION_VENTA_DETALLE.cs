using NovaBooks.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_DEVOLUCION_VENTA_DETALLE
    {
        public int DVD_DevolucionVentaDetalle { get; set; }

        public int DVD_DevolucionVentaId { get; set; }

        public int DVD_VentaDetalleId { get; set; }

        public int DVD_LibroId { get; set; }

        public int DVD_MotivoDevolucionId { get; set; }

        public decimal DVD_Cantidad { get; set; }

        public decimal DVD_PrecioUnitario { get; set; }

        public decimal DVD_MontoImpuesto { get; set; }

        public decimal DVD_Total { get; set; }

        public bool DVD_ReintegraInventario { get; set; }

        public string? DVD_Observaciones { get; set; }

        public PB_DEVOLUCION_VENTA DevolucionVenta { get; set; } = null!;

        public PB_VENTA_DETALLE VentaDetalle { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;

        public PB_MOTIVO_DEVOLUCION MotivoDevolucion { get; set; } = null!;
    }
}
