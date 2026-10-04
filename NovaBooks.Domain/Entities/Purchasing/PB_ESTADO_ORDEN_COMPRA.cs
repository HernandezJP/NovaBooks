using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Purchasing
{
    public class PB_ESTADO_ORDEN_COMPRA
    {
        public int EOC_EstadoOrdenCompra { get; set; }

        public string EOC_Codigo { get; set; } = string.Empty;

        public string EOC_Nombre { get; set; } = string.Empty;

        public string? EOC_Descripcion { get; set; }

        public bool EOC_Activo { get; set; } = true;

        public ICollection<PB_ORDEN_COMPRA> OrdenesCompra { get; set; } = [];
    }
}
