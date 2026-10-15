using Microsoft.EntityFrameworkCore;
using TikalLibros.Infrastructure.Data;

namespace TikalLibros.Infrastructure.Services.People;

public sealed record CurrentEmployee(int Id, string Code, string Name);

/// <summary>
/// Empleado activo vinculado al usuario autenticado. Las compras,
/// recepciones y movimientos de inventario quedan a su nombre.
/// </summary>
public sealed class CurrentEmployeeResolver
{
    public const string MissingEmployeeMessage =
        "Su usuario no está vinculado a un empleado activo. " +
        "Pida a un administrador que lo vincule para registrar compras e inventario.";

    private readonly AppDbContext _context;

    public CurrentEmployeeResolver(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CurrentEmployee?> FindAsync(int userId, CancellationToken cancellationToken)
    {
        var employee = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.USU_EmpleadoId != null)
            .Join(
                _context.Empleados.Where(item => item.EMP_Activo),
                user => user.USU_EmpleadoId,
                item => item.EMP_Empleado,
                (user, item) => new
                {
                    item.EMP_Empleado,
                    item.EMP_Codigo,
                    item.Persona.PersonaNatural!.PNA_PrimerNombre,
                    item.Persona.PersonaNatural.PNA_PrimerApellido
                })
            .FirstOrDefaultAsync(cancellationToken);

        return employee is null
            ? null
            : new CurrentEmployee(
                employee.EMP_Empleado,
                employee.EMP_Codigo,
                $"{employee.PNA_PrimerNombre} {employee.PNA_PrimerApellido}");
    }
}
