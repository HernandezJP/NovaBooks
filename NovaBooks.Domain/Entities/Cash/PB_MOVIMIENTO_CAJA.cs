using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Cash
{
    public class PB_MOVIMIENTO_CAJA
    {
        public int MCA_MovimientoCaja { get; set; }

        public string MCA_Numero { get; set; } = string.Empty;

        public int MCA_AperturaCajaId { get; set; }

        public int MCA_TipoMovimientoCajaId { get; set; }

        public int MCA_MetodoPagoId { get; set; }

        public int MCA_MonedaId { get; set; }

        public int MCA_EmpleadoId { get; set; }

        public int? MCA_VentaId { get; set; }

        public int? MCA_DevolucionVentaId { get; set; }

        public DateTime MCA_Fecha { get; set; } = DateTime.UtcNow;

        public decimal MCA_TipoCambio { get; set; } = 1m;

        public decimal MCA_MontoMoneda { get; set; }

        public decimal MCA_MontoBase { get; set; }

        public string? MCA_Referencia { get; set; }

        public string? MCA_Concepto { get; set; }

        public bool MCA_Anulado { get; set; }

        public DateTime? MCA_FechaAnulacion { get; set; }

        public string? MCA_MotivoAnulacion { get; set; }

        public DateTime MCA_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_APERTURA_CAJA AperturaCaja { get; set; } = null!;

        public PB_TIPO_MOVIMIENTO_CAJA TipoMovimientoCaja { get; set; } = null!;

        public PB_METODO_PAGO MetodoPago { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public PB_VENTA? Venta { get; set; }

        public PB_DEVOLUCION_VENTA? DevolucionVenta { get; set; }

        public ICollection<PB_VENTA_PAGO> PagosVenta { get; set; } = [];

        public ICollection<PB_DEVOLUCION_PAGO> PagosDevolucion { get; set; } = [];
    }
}
