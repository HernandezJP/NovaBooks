using NovaBooks.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_AJUSTE_INVENTARIO
    {
        public int AJU_AjusteInventario { get; set; }

        public string AJU_Numero { get; set; } = string.Empty;

        public int AJU_AlmacenId { get; set; }

        public int AJU_MotivoAjusteId { get; set; }

        public int? AJU_MovimientoInventarioId { get; set; }

        public int AJU_EmpleadoId { get; set; }

        public DateTime AJU_Fecha { get; set; } = DateTime.UtcNow;

        public string? AJU_Observaciones { get; set; }

        public bool AJU_Procesado { get; set; }

        public DateTime? AJU_FechaProcesado { get; set; }

        public bool AJU_Anulado { get; set; }

        public DateTime AJU_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_MOTIVO_AJUSTE MotivoAjuste { get; set; } = null!;

        public PB_MOVIMIENTO_INVENTARIO? MovimientoInventario { get; set; }

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public ICollection<PB_AJUSTE_INVENTARIO_DETALLE> Detalles { get; set; } = [];
    }
}
