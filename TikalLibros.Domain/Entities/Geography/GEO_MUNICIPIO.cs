using System;
using System.Collections.Generic;
using System.Text;
using TikalLibros.Domain.Entities.People;

namespace TikalLibros.Domain.Entities.Geography
{
    public class GEO_MUNICIPIO
    {
        public int MUN_Municipio { get; set; }

        public int MUN_DepartamentoId { get; set; }

        public string MUN_Codigo { get; set; } = string.Empty;

        public string MUN_Nombre { get; set; } = string.Empty;

        public bool MUN_Activo { get; set; } = true;

        public GEO_DEPARTAMENTO Departamento { get; set; } = null!;

        public ICollection<PB_DIRECCION> Direcciones { get; set; } = [];
    }
}
