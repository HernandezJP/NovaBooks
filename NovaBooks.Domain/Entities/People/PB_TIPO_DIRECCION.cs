using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_TIPO_DIRECCION
    {
        public int TDI_TipoDireccion { get; set; }

        public string TDI_Codigo { get; set; } = string.Empty;

        public string TDI_Nombre { get; set; } = string.Empty;

        public bool TDI_Activo { get; set; } = true;

        public ICollection<PB_PERSONA_DIRECCION> PersonasDirecciones { get; set; } = [];
    }
}
