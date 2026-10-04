using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Cash
{
    public class PB_APERTURA_CAJA
    {
        public int ACA_AperturaCaja { get; set; }

        public int ACA_CajaId { get; set; }

        public int ACA_EmpleadoId { get; set; }

        public int ACA_MonedaId { get; set; }

        public DateTime ACA_FechaApertura { get; set; } = DateTime.UtcNow;

        public decimal ACA_MontoInicial { get; set; }

        public string? ACA_Observaciones { get; set; }

        public bool ACA_Abierta { get; set; } = true;

        public DateTime ACA_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_CAJA Caja { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public PB_CIERRE_CAJA? CierreCaja { get; set; }

        public ICollection<PB_MOVIMIENTO_CAJA> Movimientos { get; set; } = [];

        public ICollection<PB_VENTA> Ventas { get; set; } = [];

        public ICollection<PB_DEVOLUCION_VENTA> Devoluciones { get; set; } = [];
    }
}
