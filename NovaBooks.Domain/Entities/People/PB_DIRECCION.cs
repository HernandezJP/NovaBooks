using NovaBooks.Domain.Entities.Geography;
using System;
using System.Collections.Generic;
using System.Text;
using NovaBooks.Domain.Entities.Administration;

namespace NovaBooks.Domain.Entities.People
{
    public class PB_DIRECCION
    {
        public int DIR_Direccion { get; set; }

        public int DIR_MunicipioId { get; set; }

        public string? DIR_Zona { get; set; }

        public string DIR_Linea1 { get; set; } = string.Empty;

        public string? DIR_Linea2 { get; set; }

        public string? DIR_CodigoPostal { get; set; }

        public string? DIR_Referencia { get; set; }

        public bool DIR_Activo { get; set; } = true;

        public DateTime DIR_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? DIR_FechaModificacion { get; set; }

        public GEO_MUNICIPIO Municipio { get; set; } = null!;

        public ICollection<PB_PERSONA_DIRECCION> PersonasDirecciones { get; set; } = [];

        public ICollection<PB_SUCURSAL> Sucursales { get; set; } = [];
    }
}
