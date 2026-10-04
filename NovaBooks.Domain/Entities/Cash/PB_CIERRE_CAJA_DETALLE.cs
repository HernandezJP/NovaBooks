using NovaBooks.Domain.Entities.Payments;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Cash
{
    public class PB_CIERRE_CAJA_DETALLE
    {
        public int CCD_CierreCajaDetalle { get; set; }

        public int CCD_CierreCajaId { get; set; }

        public int CCD_MetodoPagoId { get; set; }

        public decimal CCD_MontoSistema { get; set; }

        public decimal CCD_MontoContado { get; set; }

        public decimal CCD_Diferencia { get; set; }

        public string? CCD_Observaciones { get; set; }

        public PB_CIERRE_CAJA CierreCaja { get; set; } = null!;

        public PB_METODO_PAGO MetodoPago { get; set; } = null!;
    }
}
