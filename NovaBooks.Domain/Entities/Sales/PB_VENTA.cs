using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Inventory;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.People;
using NovaBooks.Domain.Entities.Cash;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_VENTA
    {
        public int VEN_Venta { get; set; }

        public string VEN_Numero { get; set; } = string.Empty;

        public int? VEN_PedidoClienteId { get; set; }

        public int? VEN_ClienteId { get; set; }

        public int VEN_SucursalId { get; set; }

        public int VEN_AlmacenId { get; set; }

        public int VEN_EmpleadoId { get; set; }

        public int? VEN_AperturaCajaId { get; set; }

        public int VEN_TipoVentaId { get; set; }

        public int VEN_EstadoVentaId { get; set; }

        public int VEN_MonedaId { get; set; }

        public DateTime VEN_Fecha { get; set; } = DateTime.UtcNow;

        public decimal VEN_TipoCambio { get; set; } = 1m;

        public decimal VEN_Subtotal { get; set; }

        public decimal VEN_Descuento { get; set; }

        public decimal VEN_Impuesto { get; set; }

        public decimal VEN_Total { get; set; }

        public decimal VEN_TotalPagado { get; set; }

        public decimal VEN_CambioEntregado { get; set; }

        public DateOnly? VEN_FechaVencimientoCredito { get; set; }

        public string? VEN_Observaciones { get; set; }

        public bool VEN_InventarioProcesado { get; set; }

        public DateTime? VEN_FechaProcesado { get; set; }

        public bool VEN_Anulada { get; set; }

        public DateTime? VEN_FechaAnulacion { get; set; }

        public string? VEN_MotivoAnulacion { get; set; }

        public DateTime VEN_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? VEN_FechaModificacion { get; set; }

        public PB_PEDIDO_CLIENTE? PedidoCliente { get; set; }

        public PB_CLIENTE? Cliente { get; set; }

        public PB_SUCURSAL Sucursal { get; set; } = null!;

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public PB_APERTURA_CAJA? AperturaCaja { get; set; }

        public PB_TIPO_VENTA TipoVenta { get; set; } = null!;

        public PB_ESTADO_VENTA EstadoVenta { get; set; } = null!;

        public PB_MONEDA Moneda { get; set; } = null!;

        public ICollection<PB_VENTA_DETALLE> Detalles { get; set; } = [];

        public ICollection<PB_VENTA_PAGO> Pagos { get; set; } = [];

        public ICollection<PB_DEVOLUCION_VENTA> Devoluciones { get; set; } = [];
    }
}
