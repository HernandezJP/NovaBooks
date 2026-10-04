using NovaBooks.Domain.Entities.Configuration;
using NovaBooks.Domain.Entities.People;

namespace NovaBooks.Domain.Entities.Administration;

public class PB_SUCURSAL
{
    public int SUC_Sucursal { get; set; }

    public int SUC_EmpresaId { get; set; }

    public string SUC_Codigo { get; set; } = string.Empty;

    public string SUC_Nombre { get; set; } = string.Empty;

    public int SUC_DireccionId { get; set; }

    public string? SUC_Telefono { get; set; }

    public string? SUC_Correo { get; set; }

    public bool SUC_EsPrincipal { get; set; }

    public bool SUC_Activo { get; set; } = true;

    public DateTime SUC_FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? SUC_FechaModificacion { get; set; }

    public PB_EMPRESA Empresa { get; set; } = null!;

    public PB_DIRECCION Direccion { get; set; } = null!;

    public ICollection<PB_EMPLEADO> Empleados { get; set; } = [];
}