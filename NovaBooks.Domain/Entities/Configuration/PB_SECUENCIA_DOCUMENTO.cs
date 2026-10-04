using NovaBooks.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Configuration
{
    public class PB_SECUENCIA_DOCUMENTO
    {
        public int SEC_SecuenciaDocumento { get; set; }

        public int SEC_EmpresaId { get; set; }

        public int? SEC_SucursalId { get; set; }

        public string SEC_TipoDocumento { get; set; } = string.Empty;

        public string SEC_Serie { get; set; } = string.Empty;

        public string? SEC_Prefijo { get; set; }

        public string? SEC_Sufijo { get; set; }

        public long SEC_UltimoNumero { get; set; }

        public int SEC_LongitudNumero { get; set; } = 8;

        public bool SEC_ReiniciaAnualmente { get; set; }

        public int? SEC_UltimoAnio { get; set; }

        public bool SEC_Activo { get; set; } = true;

        public DateTime SEC_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? SEC_FechaModificacion { get; set; }

        public byte[] SEC_Version { get; set; } = [];

        public PB_EMPRESA Empresa { get; set; } = null!;

        public PB_SUCURSAL? Sucursal { get; set; }
    }
}
