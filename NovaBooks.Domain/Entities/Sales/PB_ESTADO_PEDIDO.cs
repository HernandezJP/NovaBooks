using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_ESTADO_PEDIDO
    {
        public int EPE_EstadoPedido { get; set; }

        public string EPE_Codigo { get; set; } = string.Empty;

        public string EPE_Nombre { get; set; } = string.Empty;

        public string? EPE_Descripcion { get; set; }

        public bool EPE_Activo { get; set; } = true;

        public ICollection<PB_PEDIDO_CLIENTE> Pedidos { get; set; } = [];
    }
}
