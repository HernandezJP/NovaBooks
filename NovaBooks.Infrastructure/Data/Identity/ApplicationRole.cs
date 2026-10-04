using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace NovaBooks.Infrastructure.Data.Identity
{
    public class ApplicationRole : IdentityRole<int>
    {
        public string? ROL_Descripcion { get; set; }

        public bool ROL_EsSistema { get; set; }

        public bool ROL_Activo { get; set; } = true;

        public DateTime ROL_FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? ROL_FechaModificacion { get; set; }
    }
}
