using Microsoft.AspNetCore.Identity;
using NovaBooks.Domain.Entities.Administration;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Data.Identity
{
    public class ApplicationUser : IdentityUser<int>
    {
        public int? USU_EmpleadoId { get; set; }

        public bool USU_Activo { get; set; } = true;

        public bool USU_DebeCambiarPassword { get; set; } = true;

        public DateTime? USU_FechaUltimoAcceso { get; set; }

        public DateTime? USU_FechaCambioPassword { get; set; }

        public DateTime USU_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? USU_FechaModificacion { get; set; }

        public int? USU_CreadoPorUsuarioId { get; set; }

        public int? USU_ModificadoPorUsuarioId { get; set; }

        public PB_EMPLEADO? Empleado { get; set; }
    }
}
