using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Inventory;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.Cash;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_DEVOLUCION_VENTA
    {
        public int DEV_DevolucionVenta { get; set; }

        public string DEV_Numero { get; set; } = string.Empty;

        public int DEV_VentaId { get; set; }

        public int DEV_EstadoDevolucionId { get; set; }

        public int DEV_AlmacenId { get; set; }

        public int DEV_EmpleadoId { get; set; }

        public int? DEV_AperturaCajaId { get; set; }

        public int DEV_MonedaId { get; set; }

        public DateTime DEV_Fecha { get; set; } = DateTime.UtcNow;

        public decimal DEV_TipoCambio { get; set; } = 1m;

        public decimal DEV_Subtotal { get; set; }

        public decimal DEV_Impuesto { get; set; }

        public decimal DEV_Total { get; set; }

        public string? DEV_Observaciones { get; set; }

        public bool DEV_InventarioProcesado { get; set; }

        public bool DEV_ReembolsoProcesado { get; set; }

        public DateTime? DEV_FechaProcesado { get; set; }

        public bool DEV_Anulada { get; set; }

        public DateTime? DEV_FechaAnulacion { get; set; }

        public string? DEV_MotivoAnulacion { get; set; }

        public DateTime DEV_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_VENTA Venta { get; set; } = null!;

        public PB_ESTADO_DEVOLUCION EstadoDevolucion { get; set; } = null!;

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public PB_APERTURA_CAJA? AperturaCaja { get; set; }

        public PB_MONEDA Moneda { get; set; } = null!;

        public ICollection<PB_DEVOLUCION_VENTA_DETALLE> Detalles { get; set; } = [];

        public ICollection<PB_DEVOLUCION_PAGO> Reembolsos { get; set; } = [];
    }
}
