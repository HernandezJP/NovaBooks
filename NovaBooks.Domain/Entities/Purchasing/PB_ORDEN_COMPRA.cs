using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Purchasing
{
    public class PB_ORDEN_COMPRA
    {
        public int OCO_OrdenCompra { get; set; }

        public string OCO_Numero { get; set; } = string.Empty;

        public int OCO_ProveedorId { get; set; }

        public int OCO_SucursalId { get; set; }

        public int OCO_MonedaId { get; set; }

        public int OCO_EstadoOrdenCompraId { get; set; }

        public int? OCO_EmpleadoSolicitanteId { get; set; }

        public DateTime OCO_FechaEmision { get; set; } = DateTime.UtcNow;

        public DateOnly? OCO_FechaEntregaEsperada { get; set; }

        public decimal OCO_TipoCambio { get; set; } = 1m;

        public decimal OCO_Subtotal { get; set; }

        public decimal OCO_Descuento { get; set; }

        public decimal OCO_Impuesto { get; set; }

        public decimal OCO_Total { get; set; }

        public string? OCO_Observaciones { get; set; }

        public bool OCO_Activo { get; set; } = true;

        public DateTime OCO_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? OCO_FechaModificacion { get; set; }

        public PB_PROVEEDOR Proveedor { get; set; } = null!;

        public PB_SUCURSAL Sucursal { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public PB_ESTADO_ORDEN_COMPRA EstadoOrdenCompra { get; set; } = null!;

        public PB_EMPLEADO? EmpleadoSolicitante { get; set; }

        public ICollection<PB_ORDEN_COMPRA_DETALLE> Detalles { get; set; } = [];

        public ICollection<PB_RECEPCION_COMPRA> Recepciones { get; set; } = [];
    }
}
