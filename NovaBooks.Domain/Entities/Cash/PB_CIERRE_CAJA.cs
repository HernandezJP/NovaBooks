using NovaBooks.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Cash
{
    public class PB_CIERRE_CAJA
    {
        public int CCA_CierreCaja { get; set; }

        public int CCA_AperturaCajaId { get; set; }

        public int CCA_EmpleadoId { get; set; }

        public DateTime CCA_FechaCierre { get; set; } = DateTime.UtcNow;

        public decimal CCA_MontoInicial { get; set; }

        public decimal CCA_TotalIngresosEfectivo { get; set; }

        public decimal CCA_TotalEgresosEfectivo { get; set; }

        public decimal CCA_MontoEsperado { get; set; }

        public decimal CCA_MontoContado { get; set; }

        public decimal CCA_Diferencia { get; set; }

        public string? CCA_Observaciones { get; set; }

        public DateTime CCA_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_APERTURA_CAJA AperturaCaja { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public ICollection<PB_CIERRE_CAJA_DETALLE> Detalles { get; set; } = [];
    }
}
