using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Domain.Entities.Purchasing
{
    public class PB_ESTADO_RECEPCION_COMPRA
    {
        public int ERC_EstadoRecepcionCompra { get; set; }

        public string ERC_Codigo { get; set; } = string.Empty;

        public string ERC_Nombre { get; set; } = string.Empty;

        public string? ERC_Descripcion { get; set; }

        public bool ERC_Activo { get; set; } = true;

        public ICollection<PB_RECEPCION_COMPRA> Recepciones { get; set; } = [];
    }
}
