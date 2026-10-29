namespace TikalLibros.Domain.Entities.Sales
{
    /// <summary>
    /// Factura electrónica (FEL) de una venta. Guarda los datos certificados
    /// (serie, número y autorización) y una copia del receptor y los totales
    /// tal como se emitieron. En esta versión la certificación es simulada
    /// (FAC_EsDemostracion): no tiene validez fiscal.
    /// </summary>
    public class PB_FACTURA_VENTA
    {
        public int FAC_FacturaVenta { get; set; }

        public int FAC_VentaId { get; set; }

        /// <summary>Número de autorización (UUID del DTE).</summary>
        public Guid FAC_NumeroAutorizacion { get; set; }

        /// <summary>Primeros 8 caracteres hexadecimales de la autorización.</summary>
        public string FAC_Serie { get; set; } = string.Empty;

        /// <summary>Siguientes 8 caracteres hexadecimales de la autorización, como entero.</summary>
        public long FAC_Numero { get; set; }

        public DateTime FAC_FechaEmision { get; set; }

        public DateTime FAC_FechaCertificacion { get; set; }

        public string FAC_NitEmisor { get; set; } = string.Empty;

        /// <summary>NIT del receptor, o CF (consumidor final).</summary>
        public string FAC_NitReceptor { get; set; } = string.Empty;

        public string FAC_NombreReceptor { get; set; } = string.Empty;

        public decimal FAC_Total { get; set; }

        public decimal FAC_Impuesto { get; set; }

        public bool FAC_EsDemostracion { get; set; } = true;

        public bool FAC_Anulada { get; set; }

        public DateTime? FAC_FechaAnulacion { get; set; }

        public string? FAC_MotivoAnulacion { get; set; }

        public DateTime FAC_FechaCreacion { get; set; } = DateTime.UtcNow;

        public PB_VENTA Venta { get; set; } = null!;
    }
}
