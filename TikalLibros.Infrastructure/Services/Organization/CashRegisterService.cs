using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Cash;
using TikalLibros.Infrastructure.Data;

namespace TikalLibros.Infrastructure.Services.Organization;

/// <summary>
/// Cajas de cada sucursal. Una caja abierta no se desactiva ni cambia de
/// sucursal hasta que se cierre.
/// </summary>
public sealed class CashRegisterService : ICashRegisterService
{
    private const string CodePrefix = "CAJA-";

    private readonly AppDbContext _context;

    public CashRegisterService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CashRegisterAdminResponse>> GetAllAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PB_CAJA> registers = _context.Cajas.AsNoTracking();

        if (isActive.HasValue)
        {
            registers = registers.Where(item => item.CAJ_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            registers = registers.Where(item =>
                item.CAJ_Nombre.Contains(value) ||
                item.CAJ_Codigo.Contains(value) ||
                item.Sucursal.SUC_Nombre.Contains(value));
        }

        return await Project(registers
                .OrderByDescending(item => item.Sucursal.SUC_EsPrincipal)
                .ThenBy(item => item.Sucursal.SUC_Nombre)
                .ThenBy(item => item.CAJ_Codigo))
            .ToListAsync(cancellationToken);
    }

    public Task<CashRegisterAdminResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Project(_context.Cajas.AsNoTracking().Where(item => item.CAJ_Caja == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<CashRegisterAdminResponse>> CreateAsync(
        SaveCashRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        int branchId = request.BranchId ?? 0;
        string name = request.Name.Trim();

        if (await ValidateAsync(branchId, name, null, cancellationToken) is { } error)
        {
            return error;
        }

        List<string> codes = await _context.Cajas.Select(item => item.CAJ_Codigo).ToListAsync(cancellationToken);

        PB_CAJA register = new()
        {
            CAJ_SucursalId = branchId,
            CAJ_Codigo = CatalogCodes.NextSequential(CodePrefix, codes),
            CAJ_Nombre = name,
            CAJ_Descripcion = CatalogCodes.Clean(request.Description),
            CAJ_Activo = true,
            CAJ_FechaCreacion = DateTime.UtcNow
        };

        _context.Cajas.Add(register);

        if (await SaveAsync(cancellationToken) is { } concurrent)
        {
            return concurrent;
        }

        return OperationResult<CashRegisterAdminResponse>.Success((await GetByIdAsync(register.CAJ_Caja, cancellationToken))!);
    }

    public async Task<OperationResult<CashRegisterAdminResponse>> UpdateAsync(
        int id,
        SaveCashRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_CAJA? register = await _context.Cajas.FirstOrDefaultAsync(item => item.CAJ_Caja == id, cancellationToken);

        if (register is null)
        {
            return OperationResult<CashRegisterAdminResponse>.Missing("La caja solicitada no existe.");
        }

        int branchId = request.BranchId ?? 0;
        string name = request.Name.Trim();

        if (branchId != register.CAJ_SucursalId && await IsOpenAsync(id, cancellationToken))
        {
            return OperationResult<CashRegisterAdminResponse>.Failure(
                "La caja está abierta y no puede cambiar de sucursal. Ciérrela primero.");
        }

        if (await ValidateAsync(branchId, name, id, cancellationToken) is { } error)
        {
            return error;
        }

        register.CAJ_SucursalId = branchId;
        register.CAJ_Nombre = name;
        register.CAJ_Descripcion = CatalogCodes.Clean(request.Description);
        register.CAJ_FechaModificacion = DateTime.UtcNow;

        if (await SaveAsync(cancellationToken) is { } concurrent)
        {
            return concurrent;
        }

        return OperationResult<CashRegisterAdminResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<OperationResult<CashRegisterAdminResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_CAJA? register = await _context.Cajas
            .Include(item => item.Sucursal)
            .FirstOrDefaultAsync(item => item.CAJ_Caja == id, cancellationToken);

        if (register is null)
        {
            return OperationResult<CashRegisterAdminResponse>.Missing("La caja solicitada no existe.");
        }

        if (!isActive && await IsOpenAsync(id, cancellationToken))
        {
            return OperationResult<CashRegisterAdminResponse>.Conflict(
                $"La caja {register.CAJ_Nombre} está abierta. Debe cerrarse antes de desactivarla.");
        }

        if (isActive && !register.Sucursal.SUC_Activo)
        {
            return OperationResult<CashRegisterAdminResponse>.Conflict(
                $"La sucursal {register.Sucursal.SUC_Nombre} está inactiva. Reactívela primero.");
        }

        // El historial de aperturas y cierres se conserva.
        register.CAJ_Activo = isActive;
        register.CAJ_FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<CashRegisterAdminResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    private Task<bool> IsOpenAsync(int id, CancellationToken cancellationToken) =>
        _context.AperturasCaja.AnyAsync(item => item.ACA_CajaId == id && item.ACA_Abierta, cancellationToken);

    private async Task<OperationResult<CashRegisterAdminResponse>?> ValidateAsync(int branchId, string name, int? excludingId, CancellationToken cancellationToken)
    {
        if (!await _context.Sucursales.AnyAsync(item => item.SUC_Sucursal == branchId && item.SUC_Activo, cancellationToken))
        {
            return OperationResult<CashRegisterAdminResponse>.Failure("La sucursal seleccionada no existe o está inactiva.");
        }

        var duplicate = await _context.Cajas
            .AsNoTracking()
            .Where(item => item.CAJ_Caja != excludingId && item.CAJ_SucursalId == branchId && item.CAJ_Nombre == name)
            .Select(item => new { item.CAJ_Nombre, item.CAJ_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        return duplicate is null
            ? null
            : OperationResult<CashRegisterAdminResponse>.Conflict(
                $"Ya existe una caja llamada {duplicate.CAJ_Nombre} en la sucursal" + (duplicate.CAJ_Activo ? "." : " (inactiva; puede reactivarla)."));
    }

    private async Task<OperationResult<CashRegisterAdminResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return OperationResult<CashRegisterAdminResponse>.Conflict(
                "Otra caja con el mismo nombre o código se guardó al mismo tiempo. Revise los datos e intente nuevamente.");
        }
    }

    private static IQueryable<CashRegisterAdminResponse> Project(IQueryable<PB_CAJA> registers)
    {
        return registers.Select(item => new CashRegisterAdminResponse
        {
            Id = item.CAJ_Caja,
            Code = item.CAJ_Codigo,
            Name = item.CAJ_Nombre,
            Description = item.CAJ_Descripcion,
            BranchId = item.CAJ_SucursalId,
            Branch = item.Sucursal.SUC_Nombre,
            IsActive = item.CAJ_Activo,
            IsOpen = item.Aperturas.Any(session => session.ACA_Abierta),
            OpenedBy = item.Aperturas
                .Where(session => session.ACA_Abierta)
                .Select(session => session.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                                   session.Empleado.Persona.PersonaNatural.PNA_PrimerApellido)
                .FirstOrDefault()
        });
    }
}
