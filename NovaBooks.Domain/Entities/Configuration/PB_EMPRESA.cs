using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Payments;
using NovaBooks.Domain.Entities.People;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Configuration
{
    public class PB_EMPRESA
    {
        public int EMP_Empresa { get; set; }

        public int EMP_PersonaId { get; set; }

        public int EMP_MonedaPredeterminadaId { get; set; }

        public string EMP_Codigo { get; set; } = string.Empty;

        public string EMP_NombreComercial { get; set; } = string.Empty;

        public string? EMP_LogoRuta { get; set; }

        public string? EMP_SitioWeb { get; set; }

        public string? EMP_Correo { get; set; }

        public string? EMP_Telefono { get; set; }

        public string EMP_ZonaHoraria { get; set; } = "America/Guatemala";

        public string EMP_FormatoFecha { get; set; } = "dd/MM/yyyy";

        public int EMP_DecimalesCantidad { get; set; } = 2;

        public int EMP_DecimalesPrecio { get; set; } = 2;

        public bool EMP_Activo { get; set; } = true;

        public DateTime EMP_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? EMP_FechaModificacion { get; set; }

        public PB_PERSONA Persona { get; set; } = null!;

        public PB_MONEDA MonedaPredeterminada { get; set; } = null!;

        public ICollection<PB_SUCURSAL> Sucursales { get; set; } = [];

        public ICollection<PB_PARAMETRO_SISTEMA> Parametros { get; set; } = [];

        public ICollection<PB_SECUENCIA_DOCUMENTO> Secuencias { get; set; } = [];
    }
}
