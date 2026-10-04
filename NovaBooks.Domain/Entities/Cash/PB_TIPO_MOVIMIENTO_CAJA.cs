using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Cash
{
    public class PB_TIPO_MOVIMIENTO_CAJA
    {
        public int TMC_TipoMovimientoCaja { get; set; }

        public string TMC_Codigo { get; set; } = string.Empty;

        public string TMC_Nombre { get; set; } = string.Empty;

        public string? TMC_Descripcion { get; set; }

        public short TMC_Naturaleza { get; set; }

        public bool TMC_RequiereAutorizacion { get; set; }

        public bool TMC_Activo { get; set; } = true;

        public ICollection<PB_MOVIMIENTO_CAJA> Movimientos { get; set; } = [];
    }
}
