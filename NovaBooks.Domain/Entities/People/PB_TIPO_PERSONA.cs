using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_TIPO_PERSONA
    {
        public int TPR_TipoPersona { get; set; }

        public string TPR_Codigo { get; set; } = string.Empty;

        public string TPR_Nombre { get; set; } = string.Empty;

        public string? TPR_Descripcion { get; set; }

        public bool TPR_Activo { get; set; } = true;

        public ICollection<PB_PERSONA> Personas { get; set; } = [];
    }
}
