using TikalLibros.Domain.Entities.Administration;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Domain.Entities.Payments;
using TikalLibros.Domain.Entities.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace TikalLibros.Domain.Entities.Sales
{
    public class PB_PEDIDO_CLIENTE
    {
        public int PED_PedidoCliente { get; set; }

        public string PED_Numero { get; set; } = string.Empty;

        public int PED_ClienteId { get; set; }

        public int PED_SucursalId { get; set; }

        public int PED_AlmacenId { get; set; }

        public int PED_MonedaId { get; set; }

        public int PED_EstadoPedidoId { get; set; }

        public int PED_EmpleadoId { get; set; }

        public DateTime PED_Fecha { get; set; } = DateTime.UtcNow;

        public DateTime? PED_FechaVencimientoReserva { get; set; }

        public decimal PED_TipoCambio { get; set; } = 1m;

        public decimal PED_Subtotal { get; set; }

        public decimal PED_Descuento { get; set; }

        public decimal PED_Impuesto { get; set; }

        public decimal PED_Total { get; set; }

        public string? PED_Observaciones { get; set; }

        public bool PED_Activo { get; set; } = true;

        public DateTime PED_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? PED_FechaModificacion { get; set; }

        /// <summary>Contado o crédito; decide si el pedido consume crédito del cliente.</summary>
        public int? PED_TipoVentaId { get; set; }

        /// <summary>Supervisor que aprobó o rechazó el pedido.</summary>
        public int? PED_EmpleadoAprobadorId { get; set; }

        public DateTime? PED_FechaAprobacion { get; set; }

        /// <summary>Nota de la aprobación o motivo del rechazo.</summary>
        public string? PED_ComentarioAprobacion { get; set; }

        public string? PED_MotivoCancelacion { get; set; }

        public PB_CLIENTE Cliente { get; set; } = null!;

        public PB_SUCURSAL Sucursal { get; set; } = null!;

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public PB_ESTADO_PEDIDO EstadoPedido { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public PB_TIPO_VENTA? TipoVenta { get; set; }

        public PB_EMPLEADO? EmpleadoAprobador { get; set; }

        public ICollection<PB_PEDIDO_CLIENTE_DETALLE> Detalles { get; set; } = [];

        public ICollection<PB_VENTA> Ventas { get; set; } = [];
    }
}
