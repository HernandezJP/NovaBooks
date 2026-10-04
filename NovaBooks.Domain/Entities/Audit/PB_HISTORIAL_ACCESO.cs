using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Audit
{
    public class PB_HISTORIAL_ACCESO
    {
        public long HAC_HistorialAcceso { get; set; }

        public int? HAC_UsuarioId { get; set; }

        public string? HAC_NombreUsuario { get; set; }

        public DateTime HAC_Fecha { get; set; } = DateTime.UtcNow;

        public bool HAC_Exitoso { get; set; }

        public string? HAC_MotivoFallo { get; set; }

        public string? HAC_DireccionIp { get; set; }

        public string? HAC_UserAgent { get; set; }

        public string? HAC_CorrelacionId { get; set; }
    }
}
