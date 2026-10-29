using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Categories;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Catalog;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Organization;

namespace TikalLibros.Infrastructure.Services;

/// <summary>
/// Categorías del catálogo con subcategorías. El nombre es único dentro de
/// la misma categoría padre y el código se genera a partir del nombre.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private const int CodeLength = 30;

    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PB_CATEGORIA> categories = _context.Categorias.AsNoTracking();

        if (isActive.HasValue)
        {
            categories = categories.Where(item => item.CAT_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            categories = categories.Where(item =>
                item.CAT_Nombre.Contains(value) ||
                item.CAT_Codigo.Contains(value) ||
                (item.CategoriaPadre != null && item.CategoriaPadre.CAT_Nombre.Contains(value)));
        }

        // Las subcategorías aparecen debajo de su categoría padre.
        return await Project(categories
                .OrderBy(item => item.CategoriaPadre != null ? item.CategoriaPadre.CAT_Nombre : item.CAT_Nombre)
                .ThenBy(item => item.CAT_CategoriaPadreId != null)
                .ThenBy(item => item.CAT_Nombre))
            .ToListAsync(cancellationToken);
    }

    public Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Project(_context.Categorias.AsNoTracking().Where(item => item.CAT_Categoria == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<CategoryResponse>> CreateAsync(
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        string name = request.Name.Trim();

        if (await ValidateParentAsync(null, request.ParentId, cancellationToken) is { } parentError)
        {
            return OperationResult<CategoryResponse>.Failure(parentError);
        }

        if (await FindDuplicateAsync(name, request.ParentId, null, cancellationToken) is { } conflict)
        {
            return OperationResult<CategoryResponse>.Conflict(conflict);
        }

        List<string> codes = await _context.Categorias.Select(item => item.CAT_Codigo).ToListAsync(cancellationToken);

        PB_CATEGORIA category = new()
        {
            CAT_Codigo = CatalogCodes.MakeUnique(CatalogCodes.FromName(name, CodeLength), CodeLength, codes),
            CAT_Nombre = name,
            CAT_Descripcion = CatalogCodes.Clean(request.Description),
            CAT_CategoriaPadreId = request.ParentId,
            CAT_Activo = true,
            CAT_FechaCreacion = DateTime.UtcNow
        };

        _context.Categorias.Add(category);

        if (await SaveAsync(cancellationToken) is { } concurrent)
        {
            return concurrent;
        }

        return OperationResult<CategoryResponse>.Success((await GetByIdAsync(category.CAT_Categoria, cancellationToken))!);
    }

    public async Task<OperationResult<CategoryResponse>> UpdateAsync(
        int id,
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_CATEGORIA? category = await _context.Categorias.FirstOrDefaultAsync(item => item.CAT_Categoria == id, cancellationToken);

        if (category is null)
        {
            return OperationResult<CategoryResponse>.Missing("La categoría solicitada no existe.");
        }

        string name = request.Name.Trim();

        if (request.ParentId != category.CAT_CategoriaPadreId &&
            await ValidateParentAsync(id, request.ParentId, cancellationToken) is { } parentError)
        {
            return OperationResult<CategoryResponse>.Failure(parentError);
        }

        if (await FindDuplicateAsync(name, request.ParentId, id, cancellationToken) is { } conflict)
        {
            return OperationResult<CategoryResponse>.Conflict(conflict);
        }

        category.CAT_Nombre = name;
        category.CAT_Descripcion = CatalogCodes.Clean(request.Description);
        category.CAT_CategoriaPadreId = request.ParentId;
        category.CAT_FechaModificacion = DateTime.UtcNow;

        if (await SaveAsync(cancellationToken) is { } concurrent)
        {
            return concurrent;
        }

        return OperationResult<CategoryResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<OperationResult<CategoryResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_CATEGORIA? category = await _context.Categorias
            .Include(item => item.CategoriaPadre)
            .FirstOrDefaultAsync(item => item.CAT_Categoria == id, cancellationToken);

        if (category is null)
        {
            return OperationResult<CategoryResponse>.Missing("La categoría solicitada no existe.");
        }

        if (!isActive && await _context.Categorias.AnyAsync(item => item.CAT_CategoriaPadreId == id && item.CAT_Activo, cancellationToken))
        {
            return OperationResult<CategoryResponse>.Conflict(
                $"La categoría {category.CAT_Nombre} tiene subcategorías activas. Desactívelas o muévalas a otra categoría primero.");
        }

        if (isActive && category.CategoriaPadre is { CAT_Activo: false } parent)
        {
            return OperationResult<CategoryResponse>.Conflict(
                $"La categoría padre {parent.CAT_Nombre} está inactiva. Reactívela primero.");
        }

        // Eliminación lógica: los libros que ya la tienen la conservan.
        category.CAT_Activo = isActive;
        category.CAT_FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<CategoryResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    /// <summary>La categoría padre debe existir, estar activa y no ser la misma ni una de sus subcategorías.</summary>
    private async Task<string?> ValidateParentAsync(int? id, int? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return null;
        }

        if (parentId == id)
        {
            return "Una categoría no puede ser su propia categoría padre.";
        }

        Dictionary<int, (int? ParentId, bool Active)> tree = await _context.Categorias
            .AsNoTracking()
            .ToDictionaryAsync(item => item.CAT_Categoria, item => (item.CAT_CategoriaPadreId, item.CAT_Activo), cancellationToken);

        if (!tree.TryGetValue(parentId.Value, out var parent))
        {
            return "La categoría padre seleccionada no existe.";
        }

        if (!parent.Active)
        {
            return "La categoría padre seleccionada está inactiva.";
        }

        // Recorre hacia arriba desde el padre propuesto; si llega a la misma categoría habría un ciclo.
        for (int? current = parentId; current is not null && id is not null; current = tree.GetValueOrDefault(current.Value).ParentId)
        {
            if (current == id)
            {
                return "No puede mover una categoría dentro de una de sus subcategorías.";
            }
        }

        return null;
    }

    private async Task<string?> FindDuplicateAsync(string name, int? parentId, int? excludingId, CancellationToken cancellationToken)
    {
        var duplicate = await _context.Categorias
            .AsNoTracking()
            .Where(item => item.CAT_Categoria != excludingId && item.CAT_CategoriaPadreId == parentId && item.CAT_Nombre == name)
            .Select(item => new { item.CAT_Nombre, item.CAT_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (duplicate is null)
        {
            return null;
        }

        string place = parentId is null ? "como categoría principal" : "en la misma categoría padre";

        return $"Ya existe una categoría llamada {duplicate.CAT_Nombre} {place}" +
               (duplicate.CAT_Activo ? "." : " (inactiva; puede reactivarla).");
    }

    private async Task<OperationResult<CategoryResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return OperationResult<CategoryResponse>.Conflict(
                "Otra categoría con el mismo nombre se guardó al mismo tiempo. Revise los datos e intente nuevamente.");
        }
    }

    private IQueryable<CategoryResponse> Project(IQueryable<PB_CATEGORIA> categories)
    {
        return categories.Select(item => new CategoryResponse
        {
            Id = item.CAT_Categoria,
            Code = item.CAT_Codigo,
            Name = item.CAT_Nombre,
            Description = item.CAT_Descripcion,
            ParentId = item.CAT_CategoriaPadreId,
            Parent = item.CategoriaPadre != null ? item.CategoriaPadre.CAT_Nombre : null,
            BooksCount = _context.LibrosCategorias.Count(book => book.LCA_CategoriaId == item.CAT_Categoria),
            SubcategoriesCount = item.Subcategorias.Count(),
            IsActive = item.CAT_Activo
        });
    }
}
