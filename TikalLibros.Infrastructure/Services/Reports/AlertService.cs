using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.Infrastructure.Services.People;
using TikalLibros.Infrastructure.Services.Sales;

namespace TikalLibros.Infrastructure.Services.Reports;

/// <summary>
/// Centro de alertas: situaciones que alguien debe atender, agrupadas por
/// tipo y filtradas por los permisos de quien consulta. Cada alerta enlaza a
/// la pantalla donde se resuelve. Solo se incluyen las que tienen elementos.
/// </summary>
public sealed class AlertService : IAlertService
{
    private const int ExpiringHours = 24;

    private readonly AppDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly OrderService _orders;
    private readonly CurrentEmployeeResolver _employees;

    public AlertService(AppDbContext context, IInventoryService inventory, OrderService orders, CurrentEmployeeResolver employees)
    {
        _context = context;
        _inventory = inventory;
        _orders = orders;
        _employees = employees;
    }

    public async Task<AlertsResponse> GetAsync(
        IReadOnlySet<string> permissions,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        bool Has(params string[] any) => any.Any(permissions.Contains);

        List<AlertItem> items = [];
        DateTime now = DateTime.UtcNow;
        DateTime todayStart = BusinessTime.StartOfDayUtc(BusinessTime.Today);

        void Add(string code, string severity, string title, int count, string message, string href)
        {
            if (count > 0)
            {
                items.Add(new AlertItem { Code = code, Severity = severity, Title = title, Count = count, Message = message, Href = href });
            }
        }

        if (Has(InventoryPermissions.View))
        {
            StockAlertSummaryResponse stock = await _inventory.GetAlertSummaryAsync(cancellationToken);

            Add("INVENTARIO_AGOTADO", AlertSeverity.Critical, "Libros agotados", stock.OutOfStock,
                $"{stock.OutOfStock} libro(s) sin existencia en algún almacén.", "/inventario/alertas");
            Add("INVENTARIO_BAJO_MINIMO", AlertSeverity.Warning, "Existencia bajo el mínimo", stock.BelowMinimum,
                $"{stock.BelowMinimum} libro(s) por debajo de su mínimo; conviene reponer.", "/inventario/alertas");
        }

        if (Has(OrderPermissions.Approve, OrderPermissions.View))
        {
            // Libera primero las reservas vencidas para no alertar sobre ellas.
            await _orders.ExpireReservationsAsync(cancellationToken);
        }

        if (Has(OrderPermissions.Approve))
        {
            int pending = await _context.PedidosCliente.CountAsync(
                order => order.EstadoPedido.EPE_Codigo == OrderStatus.PendingApproval, cancellationToken);

            Add("PEDIDOS_POR_APROBAR", AlertSeverity.Warning, "Pedidos por aprobar", pending,
                $"{pending} pedido(s) esperan la decisión de un supervisor.", "/pedidos/aprobaciones");
        }

        if (Has(OrderPermissions.View))
        {
            DateTime soon = now.AddHours(ExpiringHours);
            int expiring = await _context.PedidosCliente.CountAsync(
                order => order.EstadoPedido.EPE_Codigo == OrderStatus.Reserved &&
                         order.PED_FechaVencimientoReserva != null && order.PED_FechaVencimientoReserva <= soon,
                cancellationToken);

            Add("RESERVAS_POR_VENCER", AlertSeverity.Warning, "Reservas por vencer", expiring,
                $"{expiring} pedido(s) reservado(s) vencen en las próximas {ExpiringHours} horas y liberarán la mercadería.",
                $"/pedidos?estado={OrderStatus.Reserved}");
        }

        if (Has(ReportPermissions.Sales))
        {
            DateOnly today = BusinessTime.Today;
            var overdue = await _context.Ventas
                .AsNoTracking()
                .Where(sale => !sale.VEN_Anulada && sale.VEN_TotalPagado < sale.VEN_Total &&
                               sale.VEN_FechaVencimientoCredito != null && sale.VEN_FechaVencimientoCredito < today)
                .GroupBy(_ => 1)
                .Select(group => new { Count = group.Count(), Balance = group.Sum(sale => sale.VEN_Total - sale.VEN_TotalPagado) })
                .FirstOrDefaultAsync(cancellationToken);

            Add("CREDITOS_VENCIDOS", AlertSeverity.Critical, "Créditos vencidos", overdue?.Count ?? 0,
                $"{overdue?.Count ?? 0} venta(s) al crédito vencida(s) por Q{overdue?.Balance ?? 0:N2}.", "/reportes/cuentas-por-cobrar");
        }

        if (Has(CashPermissions.View))
        {
            int stale = await _context.AperturasCaja.CountAsync(
                session => session.ACA_Abierta && session.ACA_FechaApertura < todayStart, cancellationToken);

            Add("CAJAS_SIN_CERRAR", AlertSeverity.Warning, "Cajas sin cerrar", stale,
                $"{stale} caja(s) siguen abiertas desde un día anterior; deben cerrarse con su arqueo.", "/caja/aperturas");
        }
        else if (Has(CashPermissions.Open) &&
                 await _employees.FindAsync(authenticatedUserId, cancellationToken) is { } employee)
        {
            bool ownStale = await _context.AperturasCaja.AnyAsync(
                session => session.ACA_Abierta && session.ACA_EmpleadoId == employee.Id && session.ACA_FechaApertura < todayStart,
                cancellationToken);

            Add("MI_CAJA_SIN_CERRAR", AlertSeverity.Warning, "Su caja sigue abierta", ownStale ? 1 : 0,
                "Su caja está abierta desde un día anterior; ciérrela con el arqueo antes de seguir vendiendo.", "/caja");
        }

        if (Has(PurchasingPermissions.View))
        {
            DateOnly today = BusinessTime.Today;
            int late = await _context.OrdenesCompra.CountAsync(
                order => (order.EstadoOrdenCompra.EOC_Codigo == PurchaseOrderStatus.Issued ||
                          order.EstadoOrdenCompra.EOC_Codigo == PurchaseOrderStatus.Partial) &&
                         order.OCO_FechaEntregaEsperada != null && order.OCO_FechaEntregaEsperada < today,
                cancellationToken);

            Add("COMPRAS_ATRASADAS", AlertSeverity.Info, "Compras atrasadas", late,
                $"{late} orden(es) de compra pasaron su fecha de entrega sin recibirse completas.", "/compras");
        }

        // Primero las críticas.
        List<AlertItem> ordered = items
            .OrderBy(item => item.Severity switch { AlertSeverity.Critical => 0, AlertSeverity.Warning => 1, _ => 2 })
            .ThenByDescending(item => item.Count)
            .ToList();

        return new AlertsResponse
        {
            GeneratedAt = now,
            Total = ordered.Sum(item => item.Count),
            Critical = ordered.Where(item => item.Severity == AlertSeverity.Critical).Sum(item => item.Count),
            Items = ordered
        };
    }
}
