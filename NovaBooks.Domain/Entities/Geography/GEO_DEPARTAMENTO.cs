using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Geography
{
    public class GEO_DEPARTAMENTO
    {
        public int DEP_Departamento { get; set; }

        public int DEP_PaisId { get; set; }

        public string DEP_Codigo { get; set; } = string.Empty;

        public string DEP_Nombre { get; set; } = string.Empty;

        public bool DEP_Activo { get; set; } = true;

        public GEO_PAIS Pais { get; set; } = null!;

        public ICollection<GEO_MUNICIPIO> Municipios { get; set; } = [];
    }
}
