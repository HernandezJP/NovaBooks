using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_ESTADO_DEVOLUCION
    {
        public int EDV_EstadoDevolucion { get; set; }

        public string EDV_Codigo { get; set; } = string.Empty;

        public string EDV_Nombre { get; set; } = string.Empty;

        public string? EDV_Descripcion { get; set; }

        public bool EDV_Activo { get; set; } = true;

        public ICollection<PB_DEVOLUCION_VENTA> Devoluciones { get; set; } = [];
    }
}
