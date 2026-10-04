using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_ESTADO_VENTA
    {
        public int EVE_EstadoVenta { get; set; }

        public string EVE_Codigo { get; set; } = string.Empty;

        public string EVE_Nombre { get; set; } = string.Empty;

        public string? EVE_Descripcion { get; set; }

        public bool EVE_Activo { get; set; } = true;

        public ICollection<PB_VENTA> Ventas { get; set; } = []; 
    }
}
