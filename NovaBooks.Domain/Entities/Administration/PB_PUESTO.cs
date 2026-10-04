using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Administration
{
    public class PB_PUESTO
    {
        public int PUE_Puesto { get; set; }

        public string PUE_Nombre { get; set; } = string.Empty;

        public string? PUE_Descripcion { get; set; }

        public bool PUE_Activo { get; set; } = true;

        public DateTime PUE_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? PUE_FechaModificacion { get; set; }

        public ICollection<PB_EMPLEADO> Empleados { get; set; } = [];
    }
}
