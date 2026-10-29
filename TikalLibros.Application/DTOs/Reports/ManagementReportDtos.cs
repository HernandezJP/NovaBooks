namespace TikalLibros.Application.DTOs.Reports;

// ───────────────────────────── Compras ─────────────────────────────

public sealed class PurchasedBookRow
{
    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal Units { get; set; }

    public decimal Total { get; set; }
}

public sealed class PurchasesReportResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    /// <summary>Recepciones procesadas del período.</summary>
    public int ReceptionsCount { get; set; }

    public int AnnulledCount { get; set; }

    public decimal Units { get; set; }

    public decimal RejectedUnits { get; set; }

    /// <summary>Unidades aceptadas al costo neto (sin IVA).</summary>
    public decimal Total { get; set; }

    public int SupplierReturnsCount { get; set; }

    public decimal SupplierReturnsTotal { get; set; }

    /// <summary>Compras menos devoluciones al proveedor.</summary>
    public decimal NetTotal { get; set; }

    /// <summary>Órdenes emitidas o parciales con mercadería por recibir (a hoy).</summary>
    public int PendingOrdersCount { get; set; }

    public decimal PendingOrdersAmount { get; set; }

    public IReadOnlyCollection<SalesByDayRow> ByDay { get; set; } = [];

    public IReadOnlyCollection<SalesByGroupRow> BySupplier { get; set; } = [];

    public IReadOnlyCollection<PurchasedBookRow> TopBooks { get; set; } = [];
}

// ───────────────────────────── Caja ─────────────────────────────

public sealed class CashTypeRow
{
    public string Name { get; set; } = string.Empty;

    /// <summary>1 ingreso, −1 egreso.</summary>
    public int Nature { get; set; }

    public int Count { get; set; }

    public decimal Total { get; set; }
}

public sealed class CashSessionReportRow
{
    public int Id { get; set; }

    public string CashRegister { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public string Employee { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public bool IsOpen { get; set; }

    public decimal InitialAmount { get; set; }

    public decimal? Expected { get; set; }

    public decimal? Counted { get; set; }

    /// <summary>Contado − esperado: negativo es faltante, positivo sobrante.</summary>
    public decimal? Difference { get; set; }
}

public sealed class CashReportResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public int SessionsCount { get; set; }

    public int OpenSessions { get; set; }

    /// <summary>Ingresos y egresos de todos los métodos (sin anulados).</summary>
    public decimal Income { get; set; }

    public decimal Expense { get; set; }

    public decimal Shortages { get; set; }

    public decimal Overages { get; set; }

    public IReadOnlyCollection<CashTypeRow> ByType { get; set; } = [];

    /// <summary>Neto (ingresos − egresos) por método de pago.</summary>
    public IReadOnlyCollection<SalesByGroupRow> ByMethod { get; set; } = [];

    public IReadOnlyCollection<CashSessionReportRow> Sessions { get; set; } = [];
}

// ───────────────────────────── Rentabilidad ─────────────────────────────

public sealed class ProfitRow
{
    public int? Id { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Units { get; set; }

    /// <summary>Ingreso neto de IVA y de devoluciones.</summary>
    public decimal Revenue { get; set; }

    public decimal Cost { get; set; }

    public decimal Margin { get; set; }

    public decimal? MarginPercent { get; set; }
}

public sealed class ProfitabilityReportResponse
{
    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public decimal Units { get; set; }

    public decimal ReturnedUnits { get; set; }

    public decimal Revenue { get; set; }

    public decimal Cost { get; set; }

    public decimal Margin { get; set; }

    public decimal? MarginPercent { get; set; }

    public IReadOnlyCollection<ProfitRow> ByCategory { get; set; } = [];

    /// <summary>Libros ordenados por margen (los 50 primeros).</summary>
    public IReadOnlyCollection<ProfitRow> ByBook { get; set; } = [];

    public IReadOnlyCollection<ProfitRow> ByEmployee { get; set; } = [];
}

// ───────────────────────────── Cuentas por cobrar ─────────────────────────────

public static class AgingBuckets
{
    public const string Current = "Vigente";
    public const string Days30 = "1 a 30 días";
    public const string Days60 = "31 a 60 días";
    public const string Days90 = "61 a 90 días";
    public const string Over90 = "Más de 90 días";

    public static readonly string[] All = [Current, Days30, Days60, Days90, Over90];

    public static string For(int daysOverdue) => daysOverdue switch
    {
        <= 0 => Current,
        <= 30 => Days30,
        <= 60 => Days60,
        <= 90 => Days90,
        _ => Over90
    };
}

public sealed class ReceivablesQuery
{
    public int? CustomerId { get; set; }

    public bool OnlyOverdue { get; set; }
}

public sealed class ReceivableSaleRow
{
    public int SaleId { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public DateOnly DueDate { get; set; }

    public int? CustomerId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public decimal Paid { get; set; }

    public decimal Balance { get; set; }

    public int DaysOverdue { get; set; }

    public string Bucket { get; set; } = string.Empty;
}

public sealed class ReceivableCustomerRow
{
    public int? CustomerId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public decimal CreditLimit { get; set; }

    public int Sales { get; set; }

    public decimal Balance { get; set; }

    public decimal Overdue { get; set; }

    public int MaxDaysOverdue { get; set; }
}

public sealed class ReceivablesResponse
{
    public DateOnly AsOf { get; set; }

    public decimal Total { get; set; }

    public decimal Overdue { get; set; }

    public int SalesCount { get; set; }

    public int CustomersCount { get; set; }

    public IReadOnlyCollection<SalesByGroupRow> Buckets { get; set; } = [];

    public IReadOnlyCollection<ReceivableCustomerRow> Customers { get; set; } = [];

    public IReadOnlyCollection<ReceivableSaleRow> Sales { get; set; } = [];
}
