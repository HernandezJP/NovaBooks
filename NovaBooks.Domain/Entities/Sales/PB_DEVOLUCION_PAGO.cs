using NovaBooks.Domain.Entities.Payments;
using System;
using System.Collections.Generic;
using System.Text;
using NovaBooks.Domain.Entities.Cash;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_DEVOLUCION_PAGO
    {
        public int DPA_DevolucionPago { get; set; }

        public int DPA_DevolucionVentaId { get; set; }

        public int DPA_MetodoPagoId { get; set; }

        public int DPA_MonedaId { get; set; }

        public int? DPA_MovimientoCajaId { get; set; }

        public DateTime DPA_Fecha { get; set; } = DateTime.UtcNow;

        public decimal DPA_TipoCambio { get; set; } = 1m;

        public decimal DPA_MontoMoneda { get; set; }

        public decimal DPA_MontoBase { get; set; }

        public string? DPA_Referencia { get; set; }

        public string? DPA_Autorizacion { get; set; }

        public bool DPA_Anulado { get; set; }

        public PB_DEVOLUCION_VENTA DevolucionVenta { get; set; } = null!;

        public PB_METODO_PAGO MetodoPago { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public PB_MOVIMIENTO_CAJA? MovimientoCaja { get; set; }
    }
}
