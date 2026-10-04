using NovaBooks.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_ALMACEN
    {
        public int ALM_Almacen { get; set; }

        public int ALM_SucursalId { get; set; }

        public string ALM_Codigo { get; set; } = string.Empty;

        public string ALM_Nombre { get; set; } = string.Empty;

        public string? ALM_Descripcion { get; set; }

        public bool ALM_EsPrincipal { get; set; }

        public bool ALM_Activo { get; set; } = true;

        public DateTime ALM_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? ALM_FechaModificacion { get; set; }

        public PB_SUCURSAL Sucursal { get; set; } = null!;

        public ICollection<PB_EXISTENCIA> Existencias { get; set; } = [];

        public ICollection<PB_LOTE> Lotes { get; set; } = [];
    }
}
