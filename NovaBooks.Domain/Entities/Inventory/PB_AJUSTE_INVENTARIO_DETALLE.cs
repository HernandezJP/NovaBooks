using NovaBooks.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_AJUSTE_INVENTARIO_DETALLE
    {
        public int AID_AjusteInventarioDetalle { get; set; }

        public int AID_AjusteInventarioId { get; set; }

        public int AID_LibroId { get; set; }

        public decimal AID_CantidadSistema { get; set; }

        public decimal AID_CantidadFisica { get; set; }

        public decimal AID_Diferencia { get; set; }

        public decimal AID_CostoUnitario { get; set; }

        public string? AID_Observaciones { get; set; }

        public PB_AJUSTE_INVENTARIO AjusteInventario { get; set; } = null!;

        public PB_LIBRO Libro { get; set; } = null!;
    }
}
