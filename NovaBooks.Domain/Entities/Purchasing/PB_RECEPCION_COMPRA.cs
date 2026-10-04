using NovaBooks.Domain.Entities.Administration;
using NovaBooks.Domain.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Purchasing
{
    public class PB_RECEPCION_COMPRA
    {
        public int REC_RecepcionCompra { get; set; }

        public string REC_Numero { get; set; } = string.Empty;

        public int REC_OrdenCompraId { get; set; }

        public int REC_AlmacenId { get; set; }

        public int REC_EstadoRecepcionCompraId { get; set; }

        public int REC_EmpleadoId { get; set; }

        public DateTime REC_FechaRecepcion { get; set; } = DateTime.UtcNow;

        public string? REC_NumeroDocumentoProveedor { get; set; }

        public string? REC_SerieDocumentoProveedor { get; set; }

        public string? REC_Observaciones { get; set; }

        public bool REC_ActualizaInventario { get; set; }

        public DateTime? REC_FechaProcesado { get; set; }

        public DateTime REC_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? REC_FechaModificacion { get; set; }

        public PB_ORDEN_COMPRA OrdenCompra { get; set; } = null!;

        public PB_ALMACEN Almacen { get; set; } = null!;

        public PB_ESTADO_RECEPCION_COMPRA EstadoRecepcionCompra { get; set; } = null!;

        public PB_EMPLEADO Empleado { get; set; } = null!;

        public ICollection<PB_RECEPCION_COMPRA_DETALLE> Detalles { get; set; } = [];

        public ICollection<PB_MOVIMIENTO_INVENTARIO> MovimientosInventario { get; set; } = [];
    }
}
