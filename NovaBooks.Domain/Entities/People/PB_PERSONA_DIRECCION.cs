using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PERSONA_DIRECCION
    {
        public int PDI_PersonaDireccion { get; set; }

        public int PDI_PersonaId { get; set; }

        public int PDI_DireccionId { get; set; }

        public int PDI_TipoDireccionId { get; set; }

        public bool PDI_Principal { get; set; }

        public bool PDI_Activo { get; set; } = true;

        public PB_PERSONA Persona { get; set; } = null!;

        public PB_DIRECCION Direccion { get; set; } = null!;

        public PB_TIPO_DIRECCION TipoDireccion { get; set; } = null!;
    }
}
