using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PERSONA_CORREO
    {
        public int PCO_PersonaCorreo { get; set; }

        public int PCO_PersonaId { get; set; }

        public string PCO_Correo { get; set; } = string.Empty;

        public bool PCO_Principal { get; set; }

        public bool PCO_Activo { get; set; } = true;

        public PB_PERSONA Persona { get; set; } = null!;
    }
}
