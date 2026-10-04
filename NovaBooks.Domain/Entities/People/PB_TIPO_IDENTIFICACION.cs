using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_TIPO_IDENTIFICACION
    {
        public int TID_TipoIdentificacion { get; set; }

        public string TID_Codigo { get; set; } = string.Empty;

        public string TID_Nombre { get; set; } = string.Empty;

        public int? TID_LongitudMinima { get; set; }

        public int? TID_LongitudMaxima { get; set; }

        public bool TID_Activo { get; set; } = true;

        public ICollection<PB_PERSONA_IDENTIFICACION> Identificaciones { get; set; } = [];
    }
}
