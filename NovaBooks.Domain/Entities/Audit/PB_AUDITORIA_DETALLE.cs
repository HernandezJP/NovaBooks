using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Audit
{
    public class PB_AUDITORIA_DETALLE
    {
        public long ADE_AuditoriaDetalle { get; set; }

        public long ADE_AuditoriaId { get; set; }

        public string ADE_Campo { get; set; } = string.Empty;

        public string? ADE_ValorAnterior { get; set; }

        public string? ADE_ValorNuevo { get; set; }

        public PB_AUDITORIA Auditoria { get; set; } = null!;
    }
}
