using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Inventory;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
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

        public PB_CLIENTE Cliente { get; set; } = null!;

        public PB_SUCURSAL Sucursal { get; set; } = null!;

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public PB_ESTADO_PEDIDO EstadoPedido { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public ICollection<PB_PEDIDO_CLIENTE_DETALLE> Detalles { get; set; } = [];

        public ICollection<PB_VENTA> Ventas { get; set; } = [];
    }
}
