using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Infrastructure.Services.Reports;

/// <summary>
/// Indicadores del inicio. Reutiliza los reportes para que las cifras del
/// tablero coincidan con ellos, y solo calcula las secciones que el usuario
/// puede ver.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private const int TopBooks = 5;

    private readonly AppDbContext _context;
    private readonly IReportService _reports;

    public DashboardService(AppDbContext context, IReportService reports)
    {
        _context = context;
        _reports = reports;
    }

    public async Task<DashboardResponse> GetAsync(IReadOnlySet<string> permissions, CancellationToken cancellationToken = default)
    {
        bool Has(params string[] any) => any.Any(permissions.Contains);

        DateOnly today = BusinessTime.Today;
        DateOnly monthStart = new(today.Year, today.Month, 1);
        ReportPeriodQuery month = new() { From = monthStart, To = today };

        DashboardResponse response = new() { GeneratedAt = DateTime.UtcNow };

        // Las consultas comparten el DbContext: se ejecutan una tras otra.
        if (Has(ReportPermissions.Sales))
        {
            response.Sales = await SalesAsync(today, month, cancellationToken);
            ReceivablesResponse receivables = await _reports.GetReceivablesAsync(new ReceivablesQuery(), cancellationToken);
            response.Receivables = new DashboardReceivables
            {
                Total = receivables.Total,
                Overdue = receivables.Overdue,
                Customers = receivables.CustomersCount,
                Buckets = receivables.Buckets
            };
        }

        if (Has(ReportPermissions.Profitability))
        {
            ProfitabilityReportResponse profit = (await _reports.GetProfitabilityAsync(month, cancellationToken)).Data!;
            response.Profit = new DashboardProfit
            {
                Revenue = profit.Revenue,
                Cost = profit.Cost,
                Margin = profit.Margin,
                MarginPercent = profit.MarginPercent,
                ByCategory = profit.ByCategory
                    .Select(row => new SalesByGroupRow { Name = row.Name, Count = (int)row.Units, Total = row.Margin })
                    .ToList()
            };
        }

        if (Has(InventoryPermissions.View, ReportPermissions.Inventory))
        {
            InventoryValuationResponse inventory = await _reports.GetInventoryValuationAsync(new InventoryValuationQuery(), cancellationToken);
            response.Inventory = new DashboardInventory
            {
                Titles = inventory.Titles,
                Units = inventory.TotalUnits,
                Value = Has(InventoryPermissions.ViewCosts, ReportPermissions.Inventory) ? inventory.TotalValue : null,
                OutOfStock = inventory.OutOfStock,
                BelowMinimum = inventory.BelowMinimum
            };
        }

        if (Has(CashPermissions.View, ReportPermissions.Cash))
        {
            response.Cash = await CashAsync(today, cancellationToken);
        }

        if (Has(OrderPermissions.View, OrderPermissions.Approve))
        {
            PendingOrdersResponse orders = await _reports.GetPendingOrdersAsync(cancellationToken);
            PendingOrdersSummary? reserved = orders.Summary.FirstOrDefault(item => item.StatusCode == OrderStatus.Reserved);

            response.Orders = new DashboardOrders
            {
                PendingApproval = orders.Summary.FirstOrDefault(item => item.StatusCode == OrderStatus.PendingApproval)?.Count ?? 0,
                Reserved = reserved?.Count ?? 0,
                ReservedTotal = reserved?.Total ?? 0,
                ExpiringSoon = orders.ExpiringSoon
            };
        }

        if (Has(PurchasingPermissions.View, ReportPermissions.Purchases))
        {
            PurchasesReportResponse purchases = (await _reports.GetPurchasesAsync(month, cancellationToken)).Data!;
            response.Purchases = new DashboardPurchases
            {
                PendingOrders = purchases.PendingOrdersCount,
                PendingAmount = purchases.PendingOrdersAmount,
                MonthTotal = purchases.NetTotal
            };
        }

        return response;
    }

    private async Task<DashboardSales> SalesAsync(DateOnly today, ReportPeriodQuery month, CancellationToken cancellationToken)
    {
        SalesReportResponse last30 = (await _reports.GetSalesAsync(new ReportPeriodQuery { From = today.AddDays(-29), To = today }, cancellationToken)).Data!;
        SalesReportResponse current = (await _reports.GetSalesAsync(month, cancellationToken)).Data!;

        // Mismo tramo del mes anterior (del 1 al mismo día, o al último si el mes es más corto).
        DateOnly previousStart = month.From!.Value.AddMonths(-1);
        DateOnly previousEnd = previousStart.AddDays(Math.Min(today.Day, DateTime.DaysInMonth(previousStart.Year, previousStart.Month)) - 1);
        SalesReportResponse previous = (await _reports.GetSalesAsync(new ReportPeriodQuery { From = previousStart, To = previousEnd }, cancellationToken)).Data!;

        TopBooksResponse top = (await _reports.GetTopBooksAsync(new TopBooksQuery { From = month.From, To = month.To, Top = TopBooks }, false, cancellationToken)).Data!;

        SalesByDayRow? todayRow = last30.ByDay.LastOrDefault();

        return new DashboardSales
        {
            TodayTotal = todayRow?.Total ?? 0,
            TodayCount = todayRow?.Count ?? 0,
            MonthTotal = current.Total,
            MonthCount = current.SalesCount,
            PreviousMonthTotal = previous.Total,
            MonthChangePercent = previous.Total > 0
                ? decimal.Round((current.Total - previous.Total) / previous.Total * 100m, 1, MidpointRounding.AwayFromZero)
                : null,
            AverageTicket = current.AverageTicket,
            MonthReturned = current.Returned,
            Last30Days = last30.ByDay,
            Last12Months = await MonthsAsync(today, cancellationToken),
            PaymentMethods = current.ByPaymentMethod,
            TopBooks = top.Rows.Select(row => new SalesByGroupRow { Name = row.Title, Count = (int)row.Units, Total = row.Revenue }).ToList()
        };
    }

    /// <summary>Ventas de los últimos 12 meses (incluido el actual), en hora de Guatemala.</summary>
    private async Task<IReadOnlyCollection<MonthTotalRow>> MonthsAsync(DateOnly today, CancellationToken cancellationToken)
    {
        DateOnly first = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        DateTime start = BusinessTime.StartOfDayUtc(first);

        var sales = await _context.Ventas
            .AsNoTracking()
            .Where(sale => !sale.VEN_Anulada && sale.VEN_Fecha >= start)
            .Select(sale => new { sale.VEN_Fecha, sale.VEN_Total })
            .ToListAsync(cancellationToken);

        var byMonth = sales
            .GroupBy(sale => { DateOnly day = BusinessTime.ToLocalDate(sale.VEN_Fecha); return (day.Year, day.Month); })
            .ToDictionary(group => group.Key, group => (Count: group.Count(), Total: group.Sum(sale => sale.VEN_Total)));

        return Enumerable.Range(0, 12)
            .Select(offset => first.AddMonths(offset))
            .Select(monthStart => new MonthTotalRow
            {
                Year = monthStart.Year,
                Month = monthStart.Month,
                Count = byMonth.GetValueOrDefault((monthStart.Year, monthStart.Month)).Count,
                Total = byMonth.GetValueOrDefault((monthStart.Year, monthStart.Month)).Total
            })
            .ToList();
    }

    private async Task<DashboardCash> CashAsync(DateOnly today, CancellationToken cancellationToken)
    {
        DateTime start = BusinessTime.StartOfDayUtc(today);
        DateTime end = BusinessTime.EndOfDayUtcExclusive(today);

        var movements = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(movement => !movement.MCA_Anulado && movement.MCA_Fecha >= start && movement.MCA_Fecha < end)
            .GroupBy(movement => movement.TipoMovimientoCaja.TMC_Naturaleza)
            .Select(group => new { Nature = group.Key, Total = group.Sum(movement => movement.MCA_MontoBase) })
            .ToListAsync(cancellationToken);

        return new DashboardCash
        {
            OpenSessions = await _context.AperturasCaja.CountAsync(session => session.ACA_Abierta, cancellationToken),
            TodayIncome = movements.Where(row => row.Nature > 0).Sum(row => row.Total),
            TodayExpense = movements.Where(row => row.Nature < 0).Sum(row => row.Total)
        };
    }
}
