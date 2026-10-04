using NovaBooks.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Text;
using NovaBooks.Domain.Entities.Purchasing;

namespace NovaBooks.Domain.Entities.Inventory
{
    public class PB_MOVIMIENTO_INVENTARIO
    {
        public int MOV_MovimientoInventario { get; set; }

        public string MOV_Numero { get; set; } = string.Empty;

        public int MOV_TipoMovimientoId { get; set; }

        public int? MOV_AlmacenOrigenId { get; set; }

        public int? MOV_AlmacenDestinoId { get; set; }

        public int? MOV_RecepcionCompraId { get; set; }

        public int? MOV_EmpleadoId { get; set; }

        public DateTime MOV_Fecha { get; set; } = DateTime.UtcNow;

        public string? MOV_DocumentoReferencia { get; set; }

        public string? MOV_Observaciones { get; set; }

        public bool MOV_Procesado { get; set; }

        public DateTime? MOV_FechaProcesado { get; set; }

        public bool MOV_Anulado { get; set; }

        public DateTime? MOV_FechaAnulacion { get; set; }

        public string? MOV_MotivoAnulacion { get; set; }

        public DateTime MOV_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_TIPO_MOVIMIENTO_INVENTARIO TipoMovimiento { get; set; } = null!;

        public PB_ALMACEN? AlmacenOrigen { get; set; }

        public PB_ALMACEN? AlmacenDestino { get; set; }

        public PB_RECEPCION_COMPRA? RecepcionCompra { get; set; }

        public PB_EMPLEADO? Empleado { get; set; }

        public ICollection<PB_MOVIMIENTO_INVENTARIO_DETALLE> Detalles { get; set; } = [];
    }
}
