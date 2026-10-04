using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PERSONA_NATURAL
    {
        public int PNA_PersonaId { get; set; }

        public string PNA_PrimerNombre { get; set; } = string.Empty;

        public string? PNA_SegundoNombre { get; set; }

        public string? PNA_TercerNombre { get; set; }

        public string PNA_PrimerApellido { get; set; } = string.Empty;

        public string? PNA_SegundoApellido { get; set; }

        public string? PNA_ApellidoCasada { get; set; }

        public DateOnly? PNA_FechaNacimiento { get; set; }

        public PB_PERSONA Persona { get; set; } = null!;
    }
}
