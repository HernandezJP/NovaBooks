using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Audit;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Audit;
using TikalLibros.Infrastructure.Data;

namespace TikalLibros.Infrastructure.Services.Audit;

/// <summary>Consulta de PB_AUDITORIA y PB_HISTORIAL_ACCESO; nunca modifica registros.</summary>
public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AuditCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default)
    {
        return new AuditCatalogsResponse
        {
            Actions = await _context.Auditorias
                .AsNoTracking()
                .Select(item => item.AUD_Accion)
                .Distinct()
                .OrderBy(item => item)
                .ToListAsync(cancellationToken),
            Entities = await _context.Auditorias
                .AsNoTracking()
                .Select(item => item.AUD_Entidad)
                .Distinct()
                .OrderBy(item => item)
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PagedResponse<AuditEntryResponse>> GetPagedAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 20, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_AUDITORIA> entries = _context.Auditorias.AsNoTracking();

        if (query.UserId.HasValue)
        {
            entries = entries.Where(item => item.AUD_UsuarioId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            string action = query.Action.Trim().ToUpperInvariant();
            entries = entries.Where(item => item.AUD_Accion == action);
        }

        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            string entity = query.Entity.Trim();
            entries = entries.Where(item => item.AUD_Entidad == entity);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            string entityId = query.EntityId.Trim();
            entries = entries.Where(item => item.AUD_EntidadId == entityId);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            entries = entries.Where(item => item.AUD_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            entries = entries.Where(item => item.AUD_Fecha < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            entries = entries.Where(item =>
                (item.AUD_Descripcion != null && item.AUD_Descripcion.Contains(value)) ||
                (item.AUD_EntidadId != null && item.AUD_EntidadId == value));
        }

        int totalItems = await entries.CountAsync(cancellationToken);

        List<AuditEntryResponse> items = await Project(entries)
            .OrderByDescending(item => item.Date)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<AuditEntryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<AuditDetailResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        AuditEntryResponse? entry = await Project(_context.Auditorias.AsNoTracking().Where(item => item.AUD_Auditoria == id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return null;
        }

        var extra = await _context.Auditorias
            .AsNoTracking()
            .Where(item => item.AUD_Auditoria == id)
            .Select(item => new
            {
                item.AUD_UserAgent,
                item.AUD_CorrelacionId,
                Changes = item.Detalles
                    .OrderBy(change => change.ADE_AuditoriaDetalle)
                    .Select(change => new AuditChangeResponse
                    {
                        Field = change.ADE_Campo,
                        Before = change.ADE_ValorAnterior,
                        After = change.ADE_ValorNuevo
                    })
                    .ToList()
            })
            .SingleAsync(cancellationToken);

        return new AuditDetailResponse
        {
            Id = entry.Id,
            Date = entry.Date,
            UserId = entry.UserId,
            UserName = entry.UserName,
            Employee = entry.Employee,
            Action = entry.Action,
            Entity = entry.Entity,
            EntityId = entry.EntityId,
            Description = entry.Description,
            IpAddress = entry.IpAddress,
            HttpMethod = entry.HttpMethod,
            Path = entry.Path,
            ChangeCount = entry.ChangeCount,
            UserAgent = extra.AUD_UserAgent,
            CorrelationId = extra.AUD_CorrelacionId,
            Changes = extra.Changes
        };
    }

    public async Task<PagedResponse<AccessLogResponse>> GetAccessLogAsync(
        AccessLogQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 20, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_HISTORIAL_ACCESO> accesses = _context.HistorialAccesos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.UserName))
        {
            string value = query.UserName.Trim();
            accesses = accesses.Where(item => item.HAC_NombreUsuario != null && item.HAC_NombreUsuario.Contains(value));
        }

        if (query.Succeeded.HasValue)
        {
            accesses = accesses.Where(item => item.HAC_Exitoso == query.Succeeded.Value);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            accesses = accesses.Where(item => item.HAC_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            accesses = accesses.Where(item => item.HAC_Fecha < to);
        }

        int totalItems = await accesses.CountAsync(cancellationToken);

        List<AccessLogResponse> items = await accesses
            .OrderByDescending(item => item.HAC_Fecha)
            .ThenByDescending(item => item.HAC_HistorialAcceso)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AccessLogResponse
            {
                Id = item.HAC_HistorialAcceso,
                Date = item.HAC_Fecha,
                UserId = item.HAC_UsuarioId,
                UserName = item.HAC_NombreUsuario,
                Succeeded = item.HAC_Exitoso,
                FailureReason = item.HAC_MotivoFallo,
                IpAddress = item.HAC_DireccionIp,
                UserAgent = item.HAC_UserAgent
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<AccessLogResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    /// <summary>Registro con el usuario (y su empleado) que hizo la operación.</summary>
    private IQueryable<AuditEntryResponse> Project(IQueryable<PB_AUDITORIA> entries) =>
        from item in entries
        join user in _context.Users on item.AUD_UsuarioId equals user.Id into users
        from user in users.DefaultIfEmpty()
        join employee in _context.Empleados on user.USU_EmpleadoId equals employee.EMP_Empleado into employees
        from employee in employees.DefaultIfEmpty()
        select new AuditEntryResponse
        {
            Id = item.AUD_Auditoria,
            Date = item.AUD_Fecha,
            UserId = item.AUD_UsuarioId,
            UserName = user != null ? user.UserName : null,
            Employee = employee != null
                ? employee.Persona.PersonaNatural!.PNA_PrimerNombre + " " + employee.Persona.PersonaNatural.PNA_PrimerApellido
                : null,
            Action = item.AUD_Accion,
            Entity = item.AUD_Entidad,
            EntityId = item.AUD_EntidadId,
            Description = item.AUD_Descripcion,
            IpAddress = item.AUD_DireccionIp,
            HttpMethod = item.AUD_MetodoHttp,
            Path = item.AUD_Ruta,
            ChangeCount = item.Detalles.Count
        };
}
