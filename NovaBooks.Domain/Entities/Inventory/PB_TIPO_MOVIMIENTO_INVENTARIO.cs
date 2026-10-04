using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_TIPO_MOVIMIENTO_INVENTARIO
    {
        public int TMI_TipoMovimientoInventario { get; set; }

        public string TMI_Codigo { get; set; } = string.Empty;

        public string TMI_Nombre { get; set; } = string.Empty;

        public string? TMI_Descripcion { get; set; }

        public short TMI_Naturaleza { get; set; }

        public bool TMI_RequiereAlmacenOrigen { get; set; }

        public bool TMI_RequiereAlmacenDestino { get; set; }

        public bool TMI_Activo { get; set; } = true;

        public ICollection<PB_MOVIMIENTO_INVENTARIO> Movimientos { get; set; } = [];
    }
}
