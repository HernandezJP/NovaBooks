using System;
using System.Collections.Generic;
using System.Text;
using NovaBooks.Domain.Entities.Cash;
using NovaBooks.Domain.Entities.Sales;

namespace NovaBooks.Domain.Entities.Payments
{
    public class PB_METODO_PAGO
    {
        public int MPA_MetodoPago { get; set; }

        public string MPA_Codigo { get; set; } = string.Empty;

        public string MPA_Nombre { get; set; } = string.Empty;

        public string? MPA_Descripcion { get; set; }

        public bool MPA_RequiereReferencia { get; set; }

        public bool MPA_RequiereAutorizacion { get; set; }

        public bool MPA_AfectaEfectivo { get; set; }

        public bool MPA_PermiteCambio { get; set; }

        public bool MPA_Activo { get; set; } = true;

        public DateTime MPA_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? MPA_FechaModificacion { get; set; }

        public ICollection<PB_VENTA_PAGO> PagosVenta { get; set; } = [];

        public ICollection<PB_DEVOLUCION_PAGO> PagosDevolucion { get; set; } = [];

        public ICollection<PB_MOVIMIENTO_CAJA> MovimientosCaja { get; set; } = [];

        public ICollection<PB_CIERRE_CAJA_DETALLE> CierresCajaDetalles { get; set; } = [];
    }
}
