using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PERSONA_IDENTIFICACION
    {
        public int PID_PersonaIdentificacion { get; set; }

        public int PID_PersonaId { get; set; }

        public int PID_TipoIdentificacionId { get; set; }

        public string PID_Numero { get; set; } = string.Empty;

        public DateOnly? PID_FechaEmision { get; set; }

        public DateOnly? PID_FechaVencimiento { get; set; }

        public bool PID_Principal { get; set; }

        public bool PID_Activo { get; set; } = true;

        public PB_PERSONA Persona { get; set; } = null!;

        public PB_TIPO_IDENTIFICACION TipoIdentificacion { get; set; } = null!;
    }
}
