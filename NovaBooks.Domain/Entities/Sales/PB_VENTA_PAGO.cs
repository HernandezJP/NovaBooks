using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.Cash;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_VENTA_PAGO
    {
        public int VPA_VentaPago { get; set; }

        public int VPA_VentaId { get; set; }

        public int VPA_MetodoPagoId { get; set; }

        public int VPA_MonedaId { get; set; }

        public int? VPA_MovimientoCajaId { get; set; }

        public DateTime VPA_Fecha { get; set; } = DateTime.UtcNow;

        public decimal VPA_TipoCambio { get; set; } = 1m;

        public decimal VPA_MontoMoneda { get; set; }

        public decimal VPA_MontoBase { get; set; }

        public decimal VPA_MontoRecibido { get; set; }

        public decimal VPA_CambioEntregado { get; set; }

        public string? VPA_Referencia { get; set; }

        public string? VPA_Autorizacion { get; set; }

        public string? VPA_UltimosCuatroDigitos { get; set; }

        public bool VPA_Anulado { get; set; }

        public DateTime? VPA_FechaAnulacion { get; set; }

        public string? VPA_MotivoAnulacion { get; set; }

        public PB_VENTA Venta { get; set; } = null!;

        public PB_METODO_PAGO MetodoPago { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public PB_MOVIMIENTO_CAJA? MovimientoCaja { get; set; }
    }
}
