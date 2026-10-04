using NovaBooks.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_PEDIDO_CLIENTE_DETALLE
    {
        public int PDD_PedidoClienteDetalle { get; set; }

        public int PDD_PedidoClienteId { get; set; }

        public int PDD_LibroId { get; set; }

        public int PDD_NumeroLinea { get; set; }

        public decimal PDD_Cantidad { get; set; }

        public decimal PDD_CantidadReservada { get; set; }

        public decimal PDD_CantidadFacturada { get; set; }

        public decimal PDD_PrecioUnitario { get; set; }

        public decimal PDD_PorcentajeDescuento { get; set; }

        public decimal PDD_MontoDescuento { get; set; }

        public decimal PDD_PorcentajeImpuesto { get; set; }

        public decimal PDD_MontoImpuesto { get; set; }

        public decimal PDD_Subtotal { get; set; }

        public decimal PDD_Total { get; set; }

        public PB_PEDIDO_CLIENTE PedidoCliente { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;
    }
}
