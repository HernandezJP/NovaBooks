namespace TikalLibros.Application.DTOs.Audit;

/// <summary>Búsqueda en la auditoría: todos los filtros se combinan.</summary>
public sealed class AuditQuery
{
    /// <summary>Texto en la descripción o el id del registro afectado.</summary>
    public string? Search { get; set; }

    public int? UserId { get; set; }

    public string? Action { get; set; }

    public string? Entity { get; set; }

    public string? EntityId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public class AuditEntryResponse
{
    public long Id { get; set; }

    public DateTime Date { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Employee { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Entity { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Description { get; set; }

    public string? IpAddress { get; set; }

    public string? HttpMethod { get; set; }

    public string? Path { get; set; }

    public int ChangeCount { get; set; }
}

public sealed class AuditChangeResponse
{
    public string Field { get; set; } = string.Empty;

    public string? Before { get; set; }

    public string? After { get; set; }
}

public sealed class AuditDetailResponse : AuditEntryResponse
{
    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }

    public IReadOnlyCollection<AuditChangeResponse> Changes { get; set; } = [];
}

public sealed class AccessLogQuery
{
    public string? UserName { get; set; }

    public bool? Succeeded { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed class AccessLogResponse
{
    public long Id { get; set; }

    public DateTime Date { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

public sealed class AuditCatalogsResponse
{
    public IReadOnlyCollection<string> Actions { get; set; } = [];

    public IReadOnlyCollection<string> Entities { get; set; } = [];
}
