using System;
using System.Collections.Generic;
using System.Text;
using NovaBooks.Domain.Entities.Administration;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PERSONA
    {
        public int PER_Persona { get; set; }

        public int PER_TipoPersonaId { get; set; }

        public bool PER_Activo { get; set; } = true;

        public DateTime PER_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? PER_FechaModificacion { get; set; }

        public PB_TIPO_PERSONA TipoPersona { get; set; } = null!;

        public PB_PERSONA_NATURAL? PersonaNatural { get; set; }

        public PB_PERSONA_JURIDICA? PersonaJuridica { get; set; }

        public ICollection<PB_PERSONA_IDENTIFICACION> Identificaciones { get; set; } = [];

        public ICollection<PB_PERSONA_TELEFONO> Telefonos { get; set; } = [];

        public ICollection<PB_PERSONA_CORREO> Correos { get; set; } = [];

        public ICollection<PB_PERSONA_DIRECCION> Direcciones { get; set; } = [];

        public PB_CLIENTE? Cliente { get; set; }

        public PB_PROVEEDOR? Proveedor { get; set; }

        public PB_EMPLEADO? Empleado { get; set; }
    }
}
