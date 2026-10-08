using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Authors;
using NovaBooks.Application.Interfaces;
using NovaBooks.Domain.Entities.Catalog;
using NovaBooks.Infrastructure.Data;

namespace NovaBooks.Infrastructure.Services;

public sealed class AuthorService : IAuthorService
{
    private readonly AppDbContext _context;

    public AuthorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<AuthorResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => 10, > 100 => 100, _ => pageSize };

        IQueryable<PB_AUTOR> authors = _context.Autores.AsNoTracking();

        if (isActive.HasValue)
        {
            authors = authors.Where(author => author.AUT_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            authors = authors.Where(author =>
                author.AUT_PrimerNombre.Contains(value) ||
                author.AUT_PrimerApellido.Contains(value) ||
                (author.AUT_PrimerNombre + " " + author.AUT_PrimerApellido).Contains(value) ||
                (author.AUT_SegundoNombre != null && author.AUT_SegundoNombre.Contains(value)) ||
                (author.AUT_SegundoApellido != null && author.AUT_SegundoApellido.Contains(value)) ||
                (author.AUT_Seudonimo != null && author.AUT_Seudonimo.Contains(value)));
        }

        int totalItems = await authors.CountAsync(cancellationToken);

        List<AuthorResponse> items =
            await Project(authors
                    .OrderBy(author => author.AUT_PrimerApellido)
                    .ThenBy(author => author.AUT_PrimerNombre)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                .ToListAsync(cancellationToken);

        items.ForEach(SetDisplayName);

        return new PagedResponse<AuthorResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<AuthorResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        AuthorResponse? author =
            await Project(_context.Autores
                    .AsNoTracking()
                    .Where(item => item.AUT_Autor == id))
                .FirstOrDefaultAsync(cancellationToken);

        if (author is not null)
        {
            SetDisplayName(author);
        }

        return author;
    }

    public async Task<OperationResult<AuthorResponse>> CreateAsync(
        SaveAuthorRequest request,
        CancellationToken cancellationToken = default)
    {
        AuthorInput input = AuthorInput.From(request);

        if (await FindDuplicateAsync(input, excludingId: null, cancellationToken) is { } conflict)
        {
            return OperationResult<AuthorResponse>.Conflict(conflict);
        }

        PB_AUTOR author = new()
        {
            AUT_Activo = true,
            AUT_FechaCreacion = DateTime.UtcNow
        };

        Apply(author, input);

        _context.Autores.Add(author);

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<AuthorResponse>.Success(
            (await GetByIdAsync(author.AUT_Autor, cancellationToken))!);
    }

    public async Task<OperationResult<AuthorResponse>> UpdateAsync(
        int id,
        SaveAuthorRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_AUTOR? author =
            await _context.Autores.FirstOrDefaultAsync(
                item => item.AUT_Autor == id,
                cancellationToken);

        if (author is null)
        {
            return OperationResult<AuthorResponse>.Missing("El autor solicitado no existe.");
        }

        AuthorInput input = AuthorInput.From(request);

        if (await FindDuplicateAsync(input, excludingId: id, cancellationToken) is { } conflict)
        {
            return OperationResult<AuthorResponse>.Conflict(conflict);
        }

        Apply(author, input);
        author.AUT_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<AuthorResponse>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<OperationResult<AuthorResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_AUTOR? author =
            await _context.Autores.FirstOrDefaultAsync(
                item => item.AUT_Autor == id,
                cancellationToken);

        if (author is null)
        {
            return OperationResult<AuthorResponse>.Missing("El autor solicitado no existe.");
        }

        // Eliminación lógica: los libros que ya lo tienen lo conservan.
        author.AUT_Activo = isActive;
        author.AUT_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<AuthorResponse>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    /// <summary>
    /// Mismo nombre, apellido y seudónimo se considera el mismo autor,
    /// aunque esté inactivo.
    /// </summary>
    private async Task<string?> FindDuplicateAsync(
        AuthorInput input,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        var duplicate =
            await _context.Autores
                .AsNoTracking()
                .Where(author =>
                    author.AUT_Autor != excludingId &&
                    author.AUT_PrimerNombre == input.FirstName &&
                    author.AUT_PrimerApellido == input.LastName &&
                    author.AUT_Seudonimo == input.Pseudonym)
                .Select(author => new { author.AUT_Activo })
                .FirstOrDefaultAsync(cancellationToken);

        if (duplicate is null)
        {
            return null;
        }

        return $"Ya existe un autor llamado {input.FirstName} {input.LastName}" +
               (input.Pseudonym is null ? string.Empty : $" ({input.Pseudonym})") +
               (duplicate.AUT_Activo ? "." : " en estado inactivo; puede reactivarlo.");
    }

    private IQueryable<AuthorResponse> Project(IQueryable<PB_AUTOR> authors)
    {
        return authors.Select(author => new AuthorResponse
        {
            Id = author.AUT_Autor,
            FirstName = author.AUT_PrimerNombre,
            MiddleName = author.AUT_SegundoNombre,
            LastName = author.AUT_PrimerApellido,
            SecondLastName = author.AUT_SegundoApellido,
            Pseudonym = author.AUT_Seudonimo,
            BirthDate = author.AUT_FechaNacimiento,
            DeathDate = author.AUT_FechaFallecimiento,
            Biography = author.AUT_Biografia,
            BooksCount = _context.LibrosAutores.Count(link => link.LAU_AutorId == author.AUT_Autor),
            IsActive = author.AUT_Activo
        });
    }

    private static void SetDisplayName(AuthorResponse author)
    {
        author.DisplayName = string.IsNullOrWhiteSpace(author.Pseudonym)
            ? $"{author.FirstName} {author.LastName}".Trim()
            : author.Pseudonym;
    }

    private static void Apply(PB_AUTOR author, AuthorInput input)
    {
        author.AUT_PrimerNombre = input.FirstName;
        author.AUT_SegundoNombre = input.MiddleName;
        author.AUT_PrimerApellido = input.LastName;
        author.AUT_SegundoApellido = input.SecondLastName;
        author.AUT_Seudonimo = input.Pseudonym;
        author.AUT_FechaNacimiento = input.BirthDate;
        author.AUT_FechaFallecimiento = input.DeathDate;
        author.AUT_Biografia = input.Biography;
    }

    private sealed record AuthorInput(
        string FirstName,
        string? MiddleName,
        string LastName,
        string? SecondLastName,
        string? Pseudonym,
        DateOnly? BirthDate,
        DateOnly? DeathDate,
        string? Biography)
    {
        public static AuthorInput From(SaveAuthorRequest request) => new(
            request.FirstName.Trim(),
            Clean(request.MiddleName),
            request.LastName.Trim(),
            Clean(request.SecondLastName),
            Clean(request.Pseudonym),
            request.BirthDate,
            request.DeathDate,
            Clean(request.Biography));

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
