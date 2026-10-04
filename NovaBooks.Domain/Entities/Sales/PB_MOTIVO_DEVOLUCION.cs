using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Sales
{
    public class PB_MOTIVO_DEVOLUCION
    {
        public int MDV_MotivoDevolucion { get; set; }

        public string MDV_Codigo { get; set; } = string.Empty;

        public string MDV_Nombre { get; set; } = string.Empty;

        public string? MDV_Descripcion { get; set; }

        public bool MDV_ReintegraInventario { get; set; }

        public bool MDV_RequiereObservacion { get; set; }

        public bool MDV_Activo { get; set; } = true;

        public ICollection<PB_DEVOLUCION_VENTA_DETALLE> Detalles { get; set; } = [];
    }
}
