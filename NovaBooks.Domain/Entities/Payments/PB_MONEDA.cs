using System;
using System.Collections.Generic;
using System.Text;
using NovaBooks.Domain.Entities.Cash;
using NovaBooks.Domain.Entities.Purchasing;
using NovaBooks.Domain.Entities.Sales;

namespace NovaBooks.Domain.Entities.Payments
{
    public class PB_MONEDA
    {
        public int MON_Moneda { get; set; }

        public string MON_Codigo { get; set; } = string.Empty;

        public string MON_Nombre { get; set; } = string.Empty;

        public string MON_Simbolo { get; set; } = string.Empty;

        public int MON_Decimales { get; set; } = 2;

        public bool MON_EsPredeterminada { get; set; }

        public bool MON_Activo { get; set; } = true;

        public ICollection<PB_ORDEN_COMPRA> OrdenesCompra { get; set; } = [];

        public ICollection<PB_PEDIDO_CLIENTE> Pedidos { get; set; } = [];

        public ICollection<PB_VENTA> Ventas { get; set; } = [];

        public ICollection<PB_VENTA_PAGO> PagosVenta { get; set; } = [];

        public ICollection<PB_DEVOLUCION_VENTA> Devoluciones { get; set; } = [];

        public ICollection<PB_DEVOLUCION_PAGO> PagosDevolucion { get; set; } = [];

        public ICollection<PB_APERTURA_CAJA> AperturasCaja { get; set; } = [];

        public ICollection<PB_MOVIMIENTO_CAJA> MovimientosCaja { get; set; } = [];
    }
}
