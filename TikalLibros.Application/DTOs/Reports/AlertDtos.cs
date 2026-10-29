namespace TikalLibros.Application.DTOs.Reports;

/// <summary>Severidad de una alerta: crítica (requiere acción ya), advertencia o informativa.</summary>
public static class AlertSeverity
{
    public const string Critical = "critica";
    public const string Warning = "advertencia";
    public const string Info = "informativa";
}

public sealed class AlertItem
{
    /// <summary>Identificador estable del tipo de alerta (por ejemplo, INVENTARIO_AGOTADO).</summary>
    public string Code { get; set; } = string.Empty;

    public string Severity { get; set; } = AlertSeverity.Info;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int Count { get; set; }

    /// <summary>Pantalla donde se atiende la alerta.</summary>
    public string Href { get; set; } = string.Empty;
}

/// <summary>Alertas vigentes que el usuario puede atender según sus permisos.</summary>
public sealed class AlertsResponse
{
    public DateTime GeneratedAt { get; set; }

    /// <summary>Suma de los elementos de todas las alertas.</summary>
    public int Total { get; set; }

    public int Critical { get; set; }

    public IReadOnlyList<AlertItem> Items { get; set; } = [];
}
