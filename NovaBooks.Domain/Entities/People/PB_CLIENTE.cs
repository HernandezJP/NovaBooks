using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_CLIENTE
    {
        public int CLI_Cliente { get; set; }

        public string CLI_Codigo { get; set; } = string.Empty;

        public int CLI_PersonaId { get; set; }

        public decimal CLI_LimiteCredito { get; set; }

        public int CLI_DiasCredito { get; set; }

        public string? CLI_Observaciones { get; set; }

        public bool CLI_Activo { get; set; } = true;

        public DateTime CLI_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? CLI_FechaModificacion { get; set; }

        public PB_PERSONA Persona { get; set; } = null!;
    }
}
