using NovaBooks.Domain.Entities.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Administration
{
    public class PB_EMPLEADO
    {
        public int EMP_Empleado { get; set; }

        public string EMP_Codigo { get; set; } = string.Empty;

        public int EMP_PersonaId { get; set; }

        public int EMP_PuestoId { get; set; }

        public int EMP_SucursalId { get; set; }

        public DateOnly EMP_FechaContratacion { get; set; }

        public DateOnly? EMP_FechaFinalizacion { get; set; }

        public bool EMP_Activo { get; set; } = true;

        public DateTime EMP_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? EMP_FechaModificacion { get; set; }

        public PB_PERSONA Persona { get; set; } = null!;

        public PB_PUESTO Puesto { get; set; } = null!;

        public PB_SUCURSAL Sucursal { get; set; } = null!;
    }
}
