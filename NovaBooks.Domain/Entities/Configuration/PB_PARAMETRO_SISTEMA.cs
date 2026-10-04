using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Configuration
{
    public class PB_PARAMETRO_SISTEMA
    {
        public int PAR_ParametroSistema { get; set; }

        public int PAR_EmpresaId { get; set; }

        public string PAR_Clave { get; set; } = string.Empty;

        public string? PAR_Valor { get; set; }

        public string PAR_TipoDato { get; set; } = "STRING";

        public string? PAR_Descripcion { get; set; }

        public bool PAR_EsEditable { get; set; } = true;

        public bool PAR_Activo { get; set; } = true;

        public DateTime PAR_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? PAR_FechaModificacion { get; set; }

        public PB_EMPRESA Empresa { get; set; } = null!;
    }
}
