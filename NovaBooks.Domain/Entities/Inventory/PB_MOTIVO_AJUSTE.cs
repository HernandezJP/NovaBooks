using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_MOTIVO_AJUSTE
    {
        public int MAJ_MotivoAjuste { get; set; }

        public string MAJ_Codigo { get; set; } = string.Empty;

        public string MAJ_Nombre { get; set; } = string.Empty;

        public string? MAJ_Descripcion { get; set; }

        public short MAJ_Naturaleza { get; set; }

        public bool MAJ_RequiereObservacion { get; set; }

        public bool MAJ_Activo { get; set; } = true;

        public ICollection<PB_AJUSTE_INVENTARIO> Ajustes { get; set; } = [];
    }
}
