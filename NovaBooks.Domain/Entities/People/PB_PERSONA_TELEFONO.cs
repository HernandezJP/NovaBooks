using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PERSONA_TELEFONO
    {
        public int PTE_PersonaTelefono { get; set; }

        public int PTE_PersonaId { get; set; }

        public int PTE_TipoTelefonoId { get; set; }

        public string PTE_Numero { get; set; } = string.Empty;

        public string? PTE_Extension { get; set; }

        public bool PTE_Principal { get; set; }

        public bool PTE_Activo { get; set; } = true;

        public PB_PERSONA Persona { get; set; } = null!;

        public PB_TIPO_TELEFONO TipoTelefono { get; set; } = null!;
    }
}
