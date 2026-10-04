using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_PROVEEDOR
    {
        public int PRO_Proveedor { get; set; }

        public string PRO_Codigo { get; set; } = string.Empty;

        public int PRO_PersonaId { get; set; }

        public decimal PRO_LimiteCredito { get; set; }

        public int PRO_DiasCredito { get; set; }

        public string? PRO_Observaciones { get; set; }

        public bool PRO_Activo { get; set; } = true;

        public DateTime PRO_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? PRO_FechaModificacion { get; set; }

        public PB_PERSONA Persona { get; set; } = null!;
    }
}
