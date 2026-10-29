namespace TikalLibros.Application.DTOs.Reports;

/// <summary>Período del reporte; si no se indica, los últimos 30 días.</summary>
public class ReportPeriodQuery
{
    public const int MaxDays = 366;

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int? WarehouseId { get; set; }
}

// ───────────────────────────── Ventas por período ─────────────────────────────

public sealed class SalesByDayRow
{
    public DateOnly Date { get; set; }

    public int Count { get; set; }

    public decimal Total { get; set; }
}

public sealed class SalesByGroupRow
{
    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Total { get; set; }
}

public sealed class SalesReportResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    /// <summary>Ventas no anuladas.</summary>
    public int SalesCount { get; set; }

    public int AnnulledCount { get; set; }

    public decimal Units { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    /// <summary>IVA incluido en las ventas.</summary>
    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    public decimal AverageTicket { get; set; }

    /// <summary>Devoluciones de clientes registradas en el período (no anuladas).</summary>
    public int ReturnsCount { get; set; }

    public decimal Returned { get; set; }

    /// <summary>Total vendido menos devoluciones del período.</summary>
    public decimal NetTotal { get; set; }

    /// <summary>
    /// Lo cobrado de esas ventas (pagos no anulados), sin contar lo que las
    /// devoluciones abonaron a su saldo.
    /// </summary>
    public decimal Collected { get; set; }

    /// <summary>Saldo pendiente de esas ventas (crédito).</summary>
    public decimal Pending { get; set; }

    public IReadOnlyCollection<SalesByDayRow> ByDay { get; set; } = [];

    public IReadOnlyCollection<SalesByGroupRow> ByEmployee { get; set; } = [];

    /// <summary>Cobros por método de pago.</summary>
    public IReadOnlyCollection<SalesByGroupRow> ByPaymentMethod { get; set; } = [];

    public IReadOnlyCollection<SalesByGroupRow> BySaleType { get; set; } = [];
}

// ───────────────────────────── Libros más vendidos ─────────────────────────────

public sealed class TopBooksQuery : ReportPeriodQuery
{
    public const int MaxTop = 100;

    public int Top { get; set; } = 10;

    /// <summary>units (predeterminado) o revenue.</summary>
    public string? OrderBy { get; set; }
}

public sealed class TopBookRow
{
    public int Rank { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int SalesCount { get; set; }

    public decimal Units { get; set; }

    public decimal Discount { get; set; }

    /// <summary>Total vendido con IVA.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Costo de lo vendido (PEPS); solo con permiso de costos.</summary>
    public decimal? Cost { get; set; }

    /// <summary>Ingreso sin IVA − costo.</summary>
    public decimal? Margin { get; set; }

    public decimal? MarginPercent { get; set; }
}

public sealed class TopBooksResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public bool IncludesCosts { get; set; }

    public decimal TotalUnits { get; set; }

    public decimal TotalRevenue { get; set; }

    public IReadOnlyCollection<TopBookRow> Rows { get; set; } = [];
}

// ───────────────────────────── Inventario valorizado ─────────────────────────────

public sealed class InventoryValuationQuery
{
    public int? WarehouseId { get; set; }

    public string? Search { get; set; }

    /// <summary>AGOTADO, BAJO_MINIMO, SOBRE_MAXIMO o NORMAL.</summary>
    public string? Status { get; set; }

    public bool OnlyWithStock { get; set; }
}

public sealed class InventoryValuationRow
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Warehouse { get; set; } = string.Empty;

    public decimal Available { get; set; }

    public decimal Reserved { get; set; }

    public decimal Free { get; set; }

    public decimal Minimum { get; set; }

    public decimal AverageCost { get; set; }

    public decimal Value { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public DateTime? LastMovementAt { get; set; }
}

public sealed class InventoryValuationResponse
{
    public const int MaxRows = 5000;

    public DateTime GeneratedAt { get; set; }

    public int Titles { get; set; }

    public decimal TotalUnits { get; set; }

    public decimal TotalValue { get; set; }

    public int OutOfStock { get; set; }

    public int BelowMinimum { get; set; }

    /// <summary>Hay más existencias que las filas devueltas.</summary>
    public bool Truncated { get; set; }

    public IReadOnlyCollection<InventoryValuationRow> Rows { get; set; } = [];
}

// ───────────────────────────── Pedidos pendientes ─────────────────────────────

public sealed class PendingOrderRow
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string SaleType { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    /// <summary>Días desde que se registró.</summary>
    public int AgeDays { get; set; }

    public DateTime? ReservationExpiresAt { get; set; }

    /// <summary>La reserva vence en menos de 24 horas.</summary>
    public bool ExpiresSoon { get; set; }

    public decimal Total { get; set; }

    public string Employee { get; set; } = string.Empty;
}

public sealed class PendingOrdersSummary
{
    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Total { get; set; }
}

public sealed class PendingOrdersResponse
{
    public DateTime GeneratedAt { get; set; }

    public int ExpiringSoon { get; set; }

    public IReadOnlyCollection<PendingOrdersSummary> Summary { get; set; } = [];

    public IReadOnlyCollection<PendingOrderRow> Rows { get; set; } = [];
}
