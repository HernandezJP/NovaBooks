using NovaBooks.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Cash
{
    public class PB_CAJA
    {
        public int CAJ_Caja { get; set; }

        public int CAJ_SucursalId { get; set; }

        public string CAJ_Codigo { get; set; } = string.Empty;

        public string CAJ_Nombre { get; set; } = string.Empty;

        public string? CAJ_Descripcion { get; set; }

        public bool CAJ_Activo { get; set; } = true;

        public DateTime CAJ_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? CAJ_FechaModificacion { get; set; }

        public PB_SUCURSAL Sucursal { get; set; } = null!;

        public ICollection<PB_APERTURA_CAJA> Aperturas { get; set; } = [];
    }
}
