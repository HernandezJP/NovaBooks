using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_TIPO_VENTA
    {
        public int TVE_TipoVenta { get; set; }

        public string TVE_Codigo { get; set; } = string.Empty;

        public string TVE_Nombre { get; set; } = string.Empty;

        public string? TVE_Descripcion { get; set; }

        public bool TVE_RequiereCliente { get; set; }

        public bool TVE_GeneraCuentaPorCobrar { get; set; }

        public bool TVE_Activo { get; set; } = true;

        public ICollection<PB_VENTA> Ventas { get; set; } = [];
    }
}
