using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Audit
{
    public class PB_AUDITORIA
    {
        public long AUD_Auditoria { get; set; }

        public int? AUD_UsuarioId { get; set; }

        public string AUD_Accion { get; set; } = string.Empty;

        public string AUD_Entidad { get; set; } = string.Empty;

        public string? AUD_EntidadId { get; set; }

        public string? AUD_Descripcion { get; set; }

        public string? AUD_DireccionIp { get; set; }

        public string? AUD_UserAgent { get; set; }

        public string? AUD_Ruta { get; set; }

        public string? AUD_MetodoHttp { get; set; }

        public string? AUD_CorrelacionId { get; set; }

        public DateTime AUD_Fecha { get; set; } = DateTime.UtcNow;

        public ICollection<PB_AUDITORIA_DETALLE> Detalles { get; set; } = [];
    }
}
