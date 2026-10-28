using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Infrastructure.Data;

namespace TikalLibros.Infrastructure.Services.Organization;

/// <summary>
/// Almacenes (bodegas) de cada sucursal. Cada sucursal tiene un almacén
/// principal; un almacén con existencias o reservas no se desactiva ni cambia
/// de sucursal (primero se trasladan o ajustan sus existencias).
/// </summary>
public sealed class WarehouseService : IWarehouseService
{
    private const string CodePrefix = "ALM-";

    private readonly AppDbContext _context;

    public WarehouseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WarehouseResponse>> GetAllAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PB_ALMACEN> warehouses = _context.Almacenes.AsNoTracking();

        if (isActive.HasValue)
        {
            warehouses = warehouses.Where(item => item.ALM_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            warehouses = warehouses.Where(item =>
                item.ALM_Nombre.Contains(value) ||
                item.ALM_Codigo.Contains(value) ||
                item.Sucursal.SUC_Nombre.Contains(value));
        }

        return await Project(warehouses
                .OrderByDescending(item => item.Sucursal.SUC_EsPrincipal)
                .ThenBy(item => item.Sucursal.SUC_Nombre)
                .ThenByDescending(item => item.ALM_EsPrincipal)
                .ThenBy(item => item.ALM_Nombre))
            .ToListAsync(cancellationToken);
    }

    public Task<WarehouseResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Project(_context.Almacenes.AsNoTracking().Where(item => item.ALM_Almacen == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OperationResult<WarehouseResponse>> CreateAsync(
        SaveWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        return _context.ExecuteInTransactionAsync(async token =>
        {
            int branchId = request.BranchId ?? 0;
            string name = request.Name.Trim();

            if (await ValidateAsync(branchId, name, null, token) is { } error)
            {
                return error;
            }

            // El primer almacén de una sucursal siempre es su principal.
            bool isMain = request.IsMain ||
                          !await _context.Almacenes.AnyAsync(item => item.ALM_SucursalId == branchId && item.ALM_EsPrincipal, token);

            if (isMain)
            {
                await ClearMainAsync(branchId, token);
            }

            List<string> codes = await _context.Almacenes.Select(item => item.ALM_Codigo).ToListAsync(token);

            PB_ALMACEN warehouse = new()
            {
                ALM_SucursalId = branchId,
                ALM_Codigo = CatalogCodes.NextSequential(CodePrefix, codes),
                ALM_Nombre = name,
                ALM_Descripcion = CatalogCodes.Clean(request.Description),
                ALM_EsPrincipal = isMain,
                ALM_Activo = true,
                ALM_FechaCreacion = DateTime.UtcNow
            };

            _context.Almacenes.Add(warehouse);

            if (await SaveAsync(token) is { } concurrent)
            {
                return concurrent;
            }

            return OperationResult<WarehouseResponse>.Success((await GetByIdAsync(warehouse.ALM_Almacen, token))!);
        }, cancellationToken);
    }

    public Task<OperationResult<WarehouseResponse>> UpdateAsync(
        int id,
        SaveWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        return _context.ExecuteInTransactionAsync(async token =>
        {
            PB_ALMACEN? warehouse = await _context.Almacenes.FirstOrDefaultAsync(item => item.ALM_Almacen == id, token);

            if (warehouse is null)
            {
                return OperationResult<WarehouseResponse>.Missing("El almacén solicitado no existe.");
            }

            int branchId = request.BranchId ?? 0;
            string name = request.Name.Trim();
            bool movesBranch = branchId != warehouse.ALM_SucursalId;

            if (movesBranch && warehouse.ALM_EsPrincipal)
            {
                return OperationResult<WarehouseResponse>.Failure(
                    "Es el almacén principal de su sucursal. Marque otro almacén como principal antes de moverlo a otra sucursal.");
            }

            if (movesBranch && await HasStockAsync(id, token))
            {
                return OperationResult<WarehouseResponse>.Failure(
                    "El almacén tiene existencias o reservas y no puede cambiar de sucursal. Trasládelas a otro almacén primero.");
            }

            if (warehouse.ALM_EsPrincipal && !request.IsMain)
            {
                return OperationResult<WarehouseResponse>.Failure(
                    "Es el almacén principal de su sucursal. Para cambiarlo, marque otro almacén como principal.");
            }

            if (!warehouse.ALM_EsPrincipal && request.IsMain && !warehouse.ALM_Activo)
            {
                return OperationResult<WarehouseResponse>.Failure("Un almacén inactivo no puede ser el principal. Reactívelo primero.");
            }

            if (await ValidateAsync(branchId, name, id, token) is { } error)
            {
                return error;
            }

            bool becomesMain = request.IsMain && (!warehouse.ALM_EsPrincipal || movesBranch);

            if (becomesMain)
            {
                await ClearMainAsync(branchId, token);
            }

            warehouse.ALM_SucursalId = branchId;
            warehouse.ALM_Nombre = name;
            warehouse.ALM_Descripcion = CatalogCodes.Clean(request.Description);
            warehouse.ALM_EsPrincipal = request.IsMain;
            warehouse.ALM_FechaModificacion = DateTime.UtcNow;

            if (await SaveAsync(token) is { } concurrent)
            {
                return concurrent;
            }

            return OperationResult<WarehouseResponse>.Success((await GetByIdAsync(id, token))!);
        }, cancellationToken);
    }

    public async Task<OperationResult<WarehouseResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_ALMACEN? warehouse = await _context.Almacenes
            .Include(item => item.Sucursal)
            .FirstOrDefaultAsync(item => item.ALM_Almacen == id, cancellationToken);

        if (warehouse is null)
        {
            return OperationResult<WarehouseResponse>.Missing("El almacén solicitado no existe.");
        }

        if (!isActive && warehouse.ALM_EsPrincipal)
        {
            return OperationResult<WarehouseResponse>.Conflict(
                $"El almacén {warehouse.ALM_Nombre} es el principal de la sucursal {warehouse.Sucursal.SUC_Nombre}. Marque otro como principal primero.");
        }

        if (!isActive && await HasStockAsync(id, cancellationToken))
        {
            return OperationResult<WarehouseResponse>.Conflict(
                $"El almacén {warehouse.ALM_Nombre} tiene existencias o reservas. Trasládelas o ajústelas antes de desactivarlo.");
        }

        if (isActive && !warehouse.Sucursal.SUC_Activo)
        {
            return OperationResult<WarehouseResponse>.Conflict(
                $"La sucursal {warehouse.Sucursal.SUC_Nombre} está inactiva. Reactívela primero.");
        }

        warehouse.ALM_Activo = isActive;
        warehouse.ALM_FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<WarehouseResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    private Task<bool> HasStockAsync(int id, CancellationToken cancellationToken) =>
        _context.Existencias.AnyAsync(
            item => item.EXI_AlmacenId == id && (item.EXI_CantidadDisponible != 0 || item.EXI_CantidadReservada != 0),
            cancellationToken);

    private async Task<OperationResult<WarehouseResponse>?> ValidateAsync(int branchId, string name, int? excludingId, CancellationToken cancellationToken)
    {
        if (!await _context.Sucursales.AnyAsync(item => item.SUC_Sucursal == branchId && item.SUC_Activo, cancellationToken))
        {
            return OperationResult<WarehouseResponse>.Failure("La sucursal seleccionada no existe o está inactiva.");
        }

        var duplicate = await _context.Almacenes
            .AsNoTracking()
            .Where(item => item.ALM_Almacen != excludingId && item.ALM_SucursalId == branchId && item.ALM_Nombre == name)
            .Select(item => new { item.ALM_Nombre, item.ALM_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        return duplicate is null
            ? null
            : OperationResult<WarehouseResponse>.Conflict(
                $"Ya existe un almacén llamado {duplicate.ALM_Nombre} en la sucursal" + (duplicate.ALM_Activo ? "." : " (inactivo; puede reactivarlo)."));
    }

    /// <summary>El índice filtrado admite un solo principal por sucursal: se quita antes de marcar otro.</summary>
    private async Task ClearMainAsync(int branchId, CancellationToken cancellationToken)
    {
        foreach (PB_ALMACEN current in await _context.Almacenes.Where(item => item.ALM_SucursalId == branchId && item.ALM_EsPrincipal).ToListAsync(cancellationToken))
        {
            current.ALM_EsPrincipal = false;
            current.ALM_FechaModificacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<OperationResult<WarehouseResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return OperationResult<WarehouseResponse>.Conflict(
                "Otro almacén con el mismo nombre se guardó al mismo tiempo. Revise los datos e intente nuevamente.");
        }
    }

    private static IQueryable<WarehouseResponse> Project(IQueryable<PB_ALMACEN> warehouses)
    {
        return warehouses.Select(item => new WarehouseResponse
        {
            Id = item.ALM_Almacen,
            Code = item.ALM_Codigo,
            Name = item.ALM_Nombre,
            Description = item.ALM_Descripcion,
            BranchId = item.ALM_SucursalId,
            Branch = item.Sucursal.SUC_Nombre,
            IsMain = item.ALM_EsPrincipal,
            IsActive = item.ALM_Activo,
            BooksWithStock = item.Existencias.Count(stock => stock.EXI_CantidadDisponible != 0 || stock.EXI_CantidadReservada != 0),
            Units = item.Existencias.Sum(stock => (decimal?)stock.EXI_CantidadDisponible) ?? 0
        });
    }
}
