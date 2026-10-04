using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PERSONA_JURIDICA
    {
        public int PJU_PersonaId { get; set; }

        public string PJU_RazonSocial { get; set; } = string.Empty;

        public string? PJU_NombreComercial { get; set; }

        public DateOnly? PJU_FechaConstitucion { get; set; }

        public PB_PERSONA Persona { get; set; } = null!;
    }
}
