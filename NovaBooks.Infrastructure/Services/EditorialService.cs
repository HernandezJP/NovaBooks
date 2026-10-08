using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Editorials;
using NovaBooks.Application.Interfaces;
using NovaBooks.Application.Validation;
using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Infrastructure.Data;

namespace NovaBooks.Infrastructure.Services;

public sealed class EditorialService : IEditorialService
{
    private const string CodePrefix = "EDI-";

    private readonly AppDbContext _context;

    public EditorialService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<EditorialResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => 10, > 100 => 100, _ => pageSize };

        IQueryable<PB_EDITORIAL> editorials = _context.Editoriales.AsNoTracking();

        if (isActive.HasValue)
        {
            editorials = editorials.Where(item => item.EDI_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            editorials = editorials.Where(item =>
                item.EDI_Nombre.Contains(value) ||
                item.EDI_Codigo.Contains(value) ||
                (item.EDI_Correo != null && item.EDI_Correo.Contains(value)));
        }

        int totalItems = await editorials.CountAsync(cancellationToken);

        List<EditorialResponse> items =
            await Project(editorials
                    .OrderBy(item => item.EDI_Nombre)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                .ToListAsync(cancellationToken);

        return new PagedResponse<EditorialResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public Task<EditorialResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return Project(_context.Editoriales
                .AsNoTracking()
                .Where(item => item.EDI_Editorial == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<EditorialResponse>> CreateAsync(
        SaveEditorialRequest request,
        CancellationToken cancellationToken = default)
    {
        EditorialInput input = EditorialInput.From(request);
        // El código no se ingresa manualmente: siempre lo asigna el sistema.
        string code = await GenerateCodeAsync(cancellationToken);

        if (await FindDuplicateAsync(input, code, excludingId: null, cancellationToken) is { } conflict)
        {
            return OperationResult<EditorialResponse>.Conflict(conflict);
        }

        PB_EDITORIAL editorial = new()
        {
            EDI_Codigo = code,
            EDI_Activo = true,
            EDI_FechaCreacion = DateTime.UtcNow
        };

        Apply(editorial, input);

        _context.Editoriales.Add(editorial);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }

        return OperationResult<EditorialResponse>.Success(
            (await GetByIdAsync(editorial.EDI_Editorial, cancellationToken))!);
    }

    public async Task<OperationResult<EditorialResponse>> UpdateAsync(
        int id,
        SaveEditorialRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_EDITORIAL? editorial =
            await _context.Editoriales.FirstOrDefaultAsync(
                item => item.EDI_Editorial == id,
                cancellationToken);

        if (editorial is null)
        {
            return OperationResult<EditorialResponse>.Missing("La editorial solicitada no existe.");
        }

        EditorialInput input = EditorialInput.From(request);
        string code = editorial.EDI_Codigo;

        if (await FindDuplicateAsync(input, code, excludingId: id, cancellationToken) is { } conflict)
        {
            return OperationResult<EditorialResponse>.Conflict(conflict);
        }

        editorial.EDI_Codigo = code;
        editorial.EDI_FechaModificacion = DateTime.UtcNow;
        Apply(editorial, input);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }

        return OperationResult<EditorialResponse>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<OperationResult<EditorialResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_EDITORIAL? editorial =
            await _context.Editoriales.FirstOrDefaultAsync(
                item => item.EDI_Editorial == id,
                cancellationToken);

        if (editorial is null)
        {
            return OperationResult<EditorialResponse>.Missing("La editorial solicitada no existe.");
        }

        // Eliminación lógica: los libros que ya la tienen la conservan.
        editorial.EDI_Activo = isActive;
        editorial.EDI_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<EditorialResponse>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    /// <summary>
    /// Código y nombre son únicos, también frente a editoriales inactivas.
    /// </summary>
    private async Task<string?> FindDuplicateAsync(
        EditorialInput input,
        string code,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        var duplicate =
            await _context.Editoriales
                .AsNoTracking()
                .Where(item =>
                    item.EDI_Editorial != excludingId &&
                    (item.EDI_Codigo == code || item.EDI_Nombre == input.Name))
                .Select(item => new { item.EDI_Codigo, item.EDI_Nombre, item.EDI_Activo })
                .FirstOrDefaultAsync(cancellationToken);

        if (duplicate is null)
        {
            return null;
        }

        string suffix = duplicate.EDI_Activo ? "." : " (inactiva; puede reactivarla).";

        return string.Equals(duplicate.EDI_Codigo, code, StringComparison.OrdinalIgnoreCase)
            ? $"El código {code} ya está registrado para la editorial {duplicate.EDI_Nombre}{suffix}"
            : $"Ya existe una editorial llamada {duplicate.EDI_Nombre}{suffix}";
    }

    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        int next =
            (await _context.Editoriales.MaxAsync(
                item => (int?)item.EDI_Editorial,
                cancellationToken) ?? 0) + 1;

        while (true)
        {
            string code = $"{CodePrefix}{next:D4}";

            if (!await _context.Editoriales.AnyAsync(item => item.EDI_Codigo == code, cancellationToken))
            {
                return code;
            }

            next++;
        }
    }

    private IQueryable<EditorialResponse> Project(IQueryable<PB_EDITORIAL> editorials)
    {
        return editorials.Select(item => new EditorialResponse
        {
            Id = item.EDI_Editorial,
            Code = item.EDI_Codigo,
            Name = item.EDI_Nombre,
            Website = item.EDI_SitioWeb,
            Email = item.EDI_Correo,
            Phone = item.EDI_Telefono,
            BooksCount = _context.Libros.Count(book => book.LIB_EditorialId == item.EDI_Editorial),
            IsActive = item.EDI_Activo
        });
    }

    private static void Apply(PB_EDITORIAL editorial, EditorialInput input)
    {
        editorial.EDI_Nombre = input.Name;
        editorial.EDI_SitioWeb = input.Website;
        editorial.EDI_Correo = input.Email;
        editorial.EDI_Telefono = input.Phone;
    }

    private static OperationResult<EditorialResponse> ConcurrentConflict()
    {
        return OperationResult<EditorialResponse>.Conflict(
            "Otra editorial con el mismo código o nombre se guardó al mismo tiempo. " +
            "Revise los datos e intente nuevamente.");
    }

    private sealed record EditorialInput(
        string Name,
        string? Website,
        string? Email,
        string? Phone)
    {
        public static EditorialInput From(SaveEditorialRequest request) => new(
            request.Name.Trim(),
            Clean(request.Website),
            Clean(request.Email) is { } email ? ContactRules.NormalizeEmail(email) : null,
            Clean(request.Phone) is { } phone ? ContactRules.NormalizePhone(phone) : null);

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
