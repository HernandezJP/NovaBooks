using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Geography
{
    public class GEO_PAIS
    {
        public int PAI_Pais { get; set; }

        public string PAI_Codigo { get; set; } = string.Empty;

        public string PAI_Nombre { get; set; } = string.Empty;

        public bool PAI_Activo { get; set; } = true;

        public ICollection<GEO_DEPARTAMENTO> Departamentos { get; set; } = [];
    }
}
