using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.PriceLists;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Catalog;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Organization;

namespace TikalLibros.Infrastructure.Services;

/// <summary>
/// Listas de precio. Siempre hay exactamente una predeterminada y activa:
/// es la que ventas y pedidos proponen primero.
/// </summary>
public sealed class PriceListService : IPriceListService
{
    private const int CodeLength = 20;

    private readonly AppDbContext _context;

    public PriceListService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PriceListResponse>> GetAllAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PB_LISTA_PRECIO> lists = _context.ListasPrecio.AsNoTracking();

        if (isActive.HasValue)
        {
            lists = lists.Where(item => item.LPR_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();
            lists = lists.Where(item => item.LPR_Nombre.Contains(value) || item.LPR_Codigo.Contains(value));
        }

        return await Project(lists.OrderByDescending(item => item.LPR_EsPredeterminada).ThenBy(item => item.LPR_Nombre))
            .ToListAsync(cancellationToken);
    }

    public Task<PriceListResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Project(_context.ListasPrecio.AsNoTracking().Where(item => item.LPR_ListaPrecio == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OperationResult<PriceListResponse>> CreateAsync(
        SavePriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        return _context.ExecuteInTransactionAsync(async token =>
        {
            string name = request.Name.Trim();

            if (await FindDuplicateAsync(name, null, token) is { } conflict)
            {
                return OperationResult<PriceListResponse>.Conflict(conflict);
            }

            List<string> codes = await _context.ListasPrecio.Select(item => item.LPR_Codigo).ToListAsync(token);
            bool hasDefault = await _context.ListasPrecio.AnyAsync(item => item.LPR_EsPredeterminada, token);
            bool isDefault = request.IsDefault || !hasDefault;

            if (isDefault)
            {
                await ClearDefaultAsync(token);
            }

            PB_LISTA_PRECIO list = new()
            {
                LPR_Codigo = CatalogCodes.MakeUnique(CatalogCodes.FromName(name, CodeLength), CodeLength, codes),
                LPR_Nombre = name,
                LPR_Descripcion = CatalogCodes.Clean(request.Description),
                LPR_EsPredeterminada = isDefault,
                LPR_Activo = true,
                LPR_FechaCreacion = DateTime.UtcNow
            };

            _context.ListasPrecio.Add(list);

            if (await SaveAsync(token) is { } concurrent)
            {
                return concurrent;
            }

            return OperationResult<PriceListResponse>.Success((await GetByIdAsync(list.LPR_ListaPrecio, token))!);
        }, cancellationToken);
    }

    public Task<OperationResult<PriceListResponse>> UpdateAsync(
        int id,
        SavePriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        return _context.ExecuteInTransactionAsync(async token =>
        {
            PB_LISTA_PRECIO? list = await _context.ListasPrecio.FirstOrDefaultAsync(item => item.LPR_ListaPrecio == id, token);

            if (list is null)
            {
                return OperationResult<PriceListResponse>.Missing("La lista de precios solicitada no existe.");
            }

            if (list.LPR_EsPredeterminada && !request.IsDefault)
            {
                return OperationResult<PriceListResponse>.Failure(
                    "Esta es la lista predeterminada. Para cambiarla, marque otra lista como predeterminada.");
            }

            if (!list.LPR_EsPredeterminada && request.IsDefault && !list.LPR_Activo)
            {
                return OperationResult<PriceListResponse>.Failure("Una lista inactiva no puede ser la predeterminada. Reactívela primero.");
            }

            string name = request.Name.Trim();

            if (await FindDuplicateAsync(name, id, token) is { } conflict)
            {
                return OperationResult<PriceListResponse>.Conflict(conflict);
            }

            if (request.IsDefault && !list.LPR_EsPredeterminada)
            {
                await ClearDefaultAsync(token);
                list.LPR_EsPredeterminada = true;
            }

            list.LPR_Nombre = name;
            list.LPR_Descripcion = CatalogCodes.Clean(request.Description);
            list.LPR_FechaModificacion = DateTime.UtcNow;

            if (await SaveAsync(token) is { } concurrent)
            {
                return concurrent;
            }

            return OperationResult<PriceListResponse>.Success((await GetByIdAsync(id, token))!);
        }, cancellationToken);
    }

    public async Task<OperationResult<PriceListResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_LISTA_PRECIO? list = await _context.ListasPrecio.FirstOrDefaultAsync(item => item.LPR_ListaPrecio == id, cancellationToken);

        if (list is null)
        {
            return OperationResult<PriceListResponse>.Missing("La lista de precios solicitada no existe.");
        }

        if (!isActive && list.LPR_EsPredeterminada)
        {
            return OperationResult<PriceListResponse>.Conflict(
                $"La lista {list.LPR_Nombre} es la predeterminada y no puede desactivarse. Marque otra como predeterminada primero.");
        }

        // Los precios se conservan; la lista deja de ofrecerse en ventas y pedidos nuevos.
        list.LPR_Activo = isActive;
        list.LPR_FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<PriceListResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    /// <summary>El índice filtrado admite una sola predeterminada: se quita antes de marcar otra.</summary>
    private async Task ClearDefaultAsync(CancellationToken cancellationToken)
    {
        List<PB_LISTA_PRECIO> defaults = await _context.ListasPrecio.Where(item => item.LPR_EsPredeterminada).ToListAsync(cancellationToken);

        foreach (PB_LISTA_PRECIO current in defaults)
        {
            current.LPR_EsPredeterminada = false;
            current.LPR_FechaModificacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> FindDuplicateAsync(string name, int? excludingId, CancellationToken cancellationToken)
    {
        var duplicate = await _context.ListasPrecio
            .AsNoTracking()
            .Where(item => item.LPR_ListaPrecio != excludingId && item.LPR_Nombre == name)
            .Select(item => new { item.LPR_Nombre, item.LPR_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        return duplicate is null
            ? null
            : $"Ya existe una lista de precios llamada {duplicate.LPR_Nombre}" + (duplicate.LPR_Activo ? "." : " (inactiva; puede reactivarla).");
    }

    private async Task<OperationResult<PriceListResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return OperationResult<PriceListResponse>.Conflict(
                "Otra lista de precios con el mismo nombre se guardó al mismo tiempo. Revise los datos e intente nuevamente.");
        }
    }

    private IQueryable<PriceListResponse> Project(IQueryable<PB_LISTA_PRECIO> lists)
    {
        DateTime now = DateTime.UtcNow;

        return lists.Select(item => new PriceListResponse
        {
            Id = item.LPR_ListaPrecio,
            Code = item.LPR_Codigo,
            Name = item.LPR_Nombre,
            Description = item.LPR_Descripcion,
            IsDefault = item.LPR_EsPredeterminada,
            IsActive = item.LPR_Activo,
            BooksCount = item.Precios
                .Where(price => price.LIP_Activo && price.LIP_FechaInicio <= now && (price.LIP_FechaFin == null || price.LIP_FechaFin >= now))
                .Select(price => price.LIP_LibroId)
                .Distinct()
                .Count()
        });
    }
}
