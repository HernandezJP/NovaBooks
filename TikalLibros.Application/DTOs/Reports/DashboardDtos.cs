namespace TikalLibros.Application.DTOs.Reports;

/// <summary>
/// Indicadores del inicio. Cada sección es null cuando el usuario no tiene
/// el permiso que la respalda.
/// </summary>
public sealed class DashboardResponse
{
    public DateTime GeneratedAt { get; set; }

    public DashboardSales? Sales { get; set; }

    public DashboardProfit? Profit { get; set; }

    public DashboardReceivables? Receivables { get; set; }

    public DashboardInventory? Inventory { get; set; }

    public DashboardCash? Cash { get; set; }

    public DashboardOrders? Orders { get; set; }

    public DashboardPurchases? Purchases { get; set; }
}

public sealed class MonthTotalRow
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int Count { get; set; }

    public decimal Total { get; set; }
}

public sealed class DashboardSales
{
    public decimal TodayTotal { get; set; }

    public int TodayCount { get; set; }

    /// <summary>Mes en curso hasta hoy.</summary>
    public decimal MonthTotal { get; set; }

    public int MonthCount { get; set; }

    /// <summary>Mismo tramo de días del mes anterior, para comparar.</summary>
    public decimal PreviousMonthTotal { get; set; }

    public decimal? MonthChangePercent { get; set; }

    public decimal AverageTicket { get; set; }

    public decimal MonthReturned { get; set; }

    public IReadOnlyCollection<SalesByDayRow> Last30Days { get; set; } = [];

    public IReadOnlyCollection<MonthTotalRow> Last12Months { get; set; } = [];

    /// <summary>Cobros del mes por método de pago.</summary>
    public IReadOnlyCollection<SalesByGroupRow> PaymentMethods { get; set; } = [];

    /// <summary>Libros más vendidos del mes (Count = unidades, Total = ingreso).</summary>
    public IReadOnlyCollection<SalesByGroupRow> TopBooks { get; set; } = [];
}

public sealed class DashboardProfit
{
    public decimal Revenue { get; set; }

    public decimal Cost { get; set; }

    public decimal Margin { get; set; }

    public decimal? MarginPercent { get; set; }

    /// <summary>Margen por categoría en el mes (Total = margen).</summary>
    public IReadOnlyCollection<SalesByGroupRow> ByCategory { get; set; } = [];
}

public sealed class DashboardReceivables
{
    public decimal Total { get; set; }

    public decimal Overdue { get; set; }

    public int Customers { get; set; }

    public IReadOnlyCollection<SalesByGroupRow> Buckets { get; set; } = [];
}

public sealed class DashboardInventory
{
    public int Titles { get; set; }

    public decimal Units { get; set; }

    /// <summary>Valor al costo; solo con permiso de ver costos.</summary>
    public decimal? Value { get; set; }

    public int OutOfStock { get; set; }

    public int BelowMinimum { get; set; }
}

public sealed class DashboardCash
{
    public int OpenSessions { get; set; }

    /// <summary>Ingresos de hoy en todas las cajas (sin anulados).</summary>
    public decimal TodayIncome { get; set; }

    public decimal TodayExpense { get; set; }
}

public sealed class DashboardOrders
{
    public int PendingApproval { get; set; }

    public int Reserved { get; set; }

    public decimal ReservedTotal { get; set; }

    public int ExpiringSoon { get; set; }
}

public sealed class DashboardPurchases
{
    public int PendingOrders { get; set; }

    public decimal PendingAmount { get; set; }

    public decimal MonthTotal { get; set; }
}
