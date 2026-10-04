using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_TIPO_TELEFONO
    {
        public int TTE_TipoTelefono { get; set; }

        public string TTE_Codigo { get; set; } = string.Empty;

        public string TTE_Nombre { get; set; } = string.Empty;

        public bool TTE_Activo { get; set; } = true;

        public ICollection<PB_PERSONA_TELEFONO> Telefonos { get; set; } = [];
    }
}
