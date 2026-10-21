using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Domain.Entities.Sales;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Inventory;
using TikalLibros.Infrastructure.Services.Purchasing;
using TikalLibros.Infrastructure.Services.Sales;

namespace TikalLibros.Infrastructure.Services.Reports;

/// <summary>
/// Reportes iniciales: ventas por período, libros más vendidos, inventario
/// valorizado y pedidos pendientes. Las fechas se agrupan en hora de
/// Guatemala; las ventas anuladas no suman.
/// </summary>
public sealed class ReportService : IReportService
{
    private static readonly string[] PendingStates = [OrderStatus.PendingApproval, OrderStatus.Reserved, OrderStatus.Draft];

    private readonly AppDbContext _context;
    private readonly OrderService _orders;

    public ReportService(AppDbContext context, OrderService orders)
    {
        _context = context;
        _orders = orders;
    }

    /// <summary>Período válido (predeterminado: últimos 30 días) o el error que corresponde.</summary>
    internal static OperationResult<(DateOnly From, DateOnly To)> ResolvePeriod(ReportPeriodQuery query)
    {
        DateOnly to = query.To ?? BusinessTime.Today;
        DateOnly from = query.From ?? to.AddDays(-29);

        if (from > to)
        {
            return OperationResult<(DateOnly, DateOnly)>.Failure("La fecha inicial no puede ser posterior a la final.");
        }

        if (to.DayNumber - from.DayNumber + 1 > ReportPeriodQuery.MaxDays)
        {
            return OperationResult<(DateOnly, DateOnly)>.Failure($"El período no puede superar los {ReportPeriodQuery.MaxDays} días.");
        }

        return OperationResult<(DateOnly, DateOnly)>.Success((from, to));
    }

    private IQueryable<PB_VENTA> SalesInPeriod(DateOnly from, DateOnly to, int? warehouseId)
    {
        DateTime start = BusinessTime.StartOfDayUtc(from);
        DateTime end = BusinessTime.EndOfDayUtcExclusive(to);

        IQueryable<PB_VENTA> sales = _context.Ventas
            .AsNoTracking()
            .Where(sale => sale.VEN_Fecha >= start && sale.VEN_Fecha < end);

        return warehouseId.HasValue ? sales.Where(sale => sale.VEN_AlmacenId == warehouseId.Value) : sales;
    }

    // ───────────────────────────── Ventas por período ─────────────────────────────

    public async Task<OperationResult<SalesReportResponse>> GetSalesAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        OperationResult<(DateOnly From, DateOnly To)> period = ResolvePeriod(query);

        if (!period.Succeeded)
        {
            return period.As<SalesReportResponse>();
        }

        (DateOnly from, DateOnly to) = period.Data;
        IQueryable<PB_VENTA> sales = SalesInPeriod(from, to, query.WarehouseId);

        var rows = await sales
            .Select(sale => new
            {
                sale.VEN_Fecha,
                sale.VEN_Anulada,
                sale.VEN_Subtotal,
                sale.VEN_Descuento,
                sale.VEN_Impuesto,
                sale.VEN_Total,
                sale.VEN_TotalPagado,
                SaleType = sale.TipoVenta.TVE_Nombre,
                Employee = sale.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           sale.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                Units = sale.Detalles.Sum(line => line.VDE_Cantidad)
            })
            .ToListAsync(cancellationToken);

        var payments = await _context.VentasPagos
            .AsNoTracking()
            .Where(payment => !payment.VPA_Anulado && sales.Any(sale => sale.VEN_Venta == payment.VPA_VentaId && !sale.VEN_Anulada))
            .GroupBy(payment => payment.MetodoPago.MPA_Nombre)
            .Select(group => new SalesByGroupRow { Name = group.Key, Count = group.Count(), Total = group.Sum(payment => payment.VPA_MontoBase) })
            .ToListAsync(cancellationToken);

        var valid = rows.Where(row => !row.VEN_Anulada).ToList();
        decimal total = valid.Sum(row => row.VEN_Total);

        Dictionary<DateOnly, (int Count, decimal Total)> byDay = valid
            .GroupBy(row => BusinessTime.ToLocalDate(row.VEN_Fecha))
            .ToDictionary(group => group.Key, group => (group.Count(), group.Sum(row => row.VEN_Total)));

        return OperationResult<SalesReportResponse>.Success(new SalesReportResponse
        {
            From = from,
            To = to,
            SalesCount = valid.Count,
            AnnulledCount = rows.Count - valid.Count,
            Units = valid.Sum(row => row.Units),
            Subtotal = valid.Sum(row => row.VEN_Subtotal),
            Discount = valid.Sum(row => row.VEN_Descuento),
            Tax = valid.Sum(row => row.VEN_Impuesto),
            Total = total,
            AverageTicket = valid.Count == 0 ? 0 : LineAmounts.Round(total / valid.Count),
            Collected = valid.Sum(row => row.VEN_TotalPagado),
            Pending = valid.Sum(row => row.VEN_Total - row.VEN_TotalPagado),
            // Todos los días del período, aunque no haya ventas, para graficar.
            ByDay = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
                .Select(offset => from.AddDays(offset))
                .Select(day => new SalesByDayRow
                {
                    Date = day,
                    Count = byDay.TryGetValue(day, out var value) ? value.Count : 0,
                    Total = byDay.TryGetValue(day, out value) ? value.Total : 0
                })
                .ToList(),
            ByEmployee = Group(valid.Select(row => (row.Employee, row.VEN_Total))),
            BySaleType = Group(valid.Select(row => (row.SaleType, row.VEN_Total))),
            ByPaymentMethod = payments.OrderByDescending(item => item.Total).ToList()
        });
    }

    private static IReadOnlyCollection<SalesByGroupRow> Group(IEnumerable<(string Name, decimal Total)> rows) =>
        rows.GroupBy(row => row.Name)
            .Select(group => new SalesByGroupRow { Name = group.Key, Count = group.Count(), Total = group.Sum(row => row.Total) })
            .OrderByDescending(row => row.Total)
            .ToList();

    // ───────────────────────────── Libros más vendidos ─────────────────────────────

    public async Task<OperationResult<TopBooksResponse>> GetTopBooksAsync(
        TopBooksQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        OperationResult<(DateOnly From, DateOnly To)> period = ResolvePeriod(query);

        if (!period.Succeeded)
        {
            return period.As<TopBooksResponse>();
        }

        if (query.Top is < 1 or > TopBooksQuery.MaxTop)
        {
            return OperationResult<TopBooksResponse>.Failure($"La cantidad de libros debe estar entre 1 y {TopBooksQuery.MaxTop}.");
        }

        bool byRevenue = string.Equals(query.OrderBy, "revenue", StringComparison.OrdinalIgnoreCase);

        if (!byRevenue && query.OrderBy is not null && !string.Equals(query.OrderBy, "units", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<TopBooksResponse>.Failure("Ordene por unidades (units) o por ingresos (revenue).");
        }

        (DateOnly from, DateOnly to) = period.Data;
        IQueryable<PB_VENTA> sales = SalesInPeriod(from, to, query.WarehouseId).Where(sale => !sale.VEN_Anulada);

        var grouped = await _context.VentasDetalles
            .AsNoTracking()
            .Where(line => sales.Any(sale => sale.VEN_Venta == line.VDE_VentaId))
            .GroupBy(line => line.VDE_LibroId)
            .Select(group => new
            {
                BookId = group.Key,
                Sales = group.Select(line => line.VDE_VentaId).Distinct().Count(),
                Units = group.Sum(line => line.VDE_Cantidad),
                Discount = group.Sum(line => line.VDE_MontoDescuento),
                Revenue = group.Sum(line => line.VDE_Total),
                Tax = group.Sum(line => line.VDE_MontoImpuesto),
                Cost = group.Sum(line => line.VDE_Cantidad * line.VDE_CostoUnitario)
            })
            .ToListAsync(cancellationToken);

        var ordered = (byRevenue
                ? grouped.OrderByDescending(item => item.Revenue).ThenByDescending(item => item.Units)
                : grouped.OrderByDescending(item => item.Units).ThenByDescending(item => item.Revenue))
            .Take(query.Top)
            .ToList();

        int[] ids = ordered.Select(item => item.BookId).ToArray();
        var books = await _context.Libros
            .AsNoTracking()
            .Where(book => ids.Contains(book.LIB_Libro))
            .ToDictionaryAsync(book => book.LIB_Libro, book => new { book.LIB_Codigo, book.LIB_Titulo }, cancellationToken);

        return OperationResult<TopBooksResponse>.Success(new TopBooksResponse
        {
            From = from,
            To = to,
            IncludesCosts = includeCosts,
            TotalUnits = grouped.Sum(item => item.Units),
            TotalRevenue = grouped.Sum(item => item.Revenue),
            Rows = ordered
                .Select((item, index) =>
                {
                    decimal cost = LineAmounts.Round(item.Cost);
                    decimal net = item.Revenue - item.Tax;
                    decimal margin = net - cost;

                    return new TopBookRow
                    {
                        Rank = index + 1,
                        BookId = item.BookId,
                        BookCode = books[item.BookId].LIB_Codigo,
                        Title = books[item.BookId].LIB_Titulo,
                        SalesCount = item.Sales,
                        Units = item.Units,
                        Discount = item.Discount,
                        Revenue = item.Revenue,
                        Cost = includeCosts ? cost : null,
                        Margin = includeCosts ? margin : null,
                        MarginPercent = includeCosts && net > 0 ? decimal.Round(margin / net * 100m, 2, MidpointRounding.AwayFromZero) : null
                    };
                })
                .ToList()
        });
    }

    // ───────────────────────────── Inventario valorizado ─────────────────────────────

    public async Task<InventoryValuationResponse> GetInventoryValuationAsync(
        InventoryValuationQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PB_EXISTENCIA> stock = _context.Existencias.AsNoTracking();

        if (query.WarehouseId.HasValue)
        {
            stock = stock.Where(item => item.EXI_AlmacenId == query.WarehouseId.Value);
        }

        if (query.OnlyWithStock)
        {
            stock = stock.Where(item => item.EXI_CantidadDisponible > 0);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            stock = stock.Where(item => item.Libro.LIB_Titulo.Contains(value) || item.Libro.LIB_Codigo.Contains(value));
        }

        var rows = await stock
            .OrderBy(item => item.Libro.LIB_Titulo)
            .ThenBy(item => item.Almacen.ALM_Nombre)
            .Select(item => new
            {
                item.EXI_LibroId,
                item.Libro.LIB_Codigo,
                item.Libro.LIB_Titulo,
                item.Libro.LIB_CostoReferencia,
                item.Almacen.ALM_Nombre,
                item.EXI_CantidadDisponible,
                item.EXI_CantidadReservada,
                item.EXI_StockMinimo,
                item.EXI_StockMaximo,
                item.EXI_FechaUltimoMovimiento,
                LotUnits = _context.Lotes
                    .Where(lot => lot.LOT_AlmacenId == item.EXI_AlmacenId && lot.LOT_LibroId == item.EXI_LibroId && lot.LOT_Activo)
                    .Sum(lot => lot.LOT_CantidadActual),
                LotValue = _context.Lotes
                    .Where(lot => lot.LOT_AlmacenId == item.EXI_AlmacenId && lot.LOT_LibroId == item.EXI_LibroId && lot.LOT_Activo)
                    .Sum(lot => lot.LOT_CantidadActual * lot.LOT_CostoUnitario)
            })
            .ToListAsync(cancellationToken);

        List<InventoryValuationRow> valued = rows
            .Select(row =>
            {
                string status = InventoryService.Classify(row.EXI_CantidadDisponible, row.EXI_StockMinimo, row.EXI_StockMaximo);
                // Costo promedio de los lotes vigentes; sin lotes, el de referencia del libro.
                decimal average = row.LotUnits > 0
                    ? decimal.Round(row.LotValue / row.LotUnits, 4, MidpointRounding.AwayFromZero)
                    : row.LIB_CostoReferencia;

                return new InventoryValuationRow
                {
                    BookId = row.EXI_LibroId,
                    BookCode = row.LIB_Codigo,
                    Title = row.LIB_Titulo,
                    Warehouse = row.ALM_Nombre,
                    Available = row.EXI_CantidadDisponible,
                    Reserved = row.EXI_CantidadReservada,
                    Free = row.EXI_CantidadDisponible - row.EXI_CantidadReservada,
                    Minimum = row.EXI_StockMinimo,
                    AverageCost = average,
                    Value = LineAmounts.Round(row.LotUnits >= row.EXI_CantidadDisponible
                        ? row.LotValue
                        : row.LotValue + (row.EXI_CantidadDisponible - row.LotUnits) * average),
                    Status = status,
                    StatusLabel = StockStatus.Label(status),
                    LastMovementAt = row.EXI_FechaUltimoMovimiento
                };
            })
            .Where(row => string.IsNullOrWhiteSpace(query.Status) || row.Status == query.Status.Trim().ToUpperInvariant())
            .ToList();

        return new InventoryValuationResponse
        {
            GeneratedAt = DateTime.UtcNow,
            Titles = valued.Select(row => row.BookId).Distinct().Count(),
            TotalUnits = valued.Sum(row => row.Available),
            TotalValue = valued.Sum(row => row.Value),
            OutOfStock = valued.Count(row => row.Status == StockStatus.OutOfStock),
            BelowMinimum = valued.Count(row => row.Status == StockStatus.BelowMinimum),
            Truncated = valued.Count > InventoryValuationResponse.MaxRows,
            Rows = valued.Take(InventoryValuationResponse.MaxRows).ToList()
        };
    }

    // ───────────────────────────── Pedidos pendientes ─────────────────────────────

    public async Task<PendingOrdersResponse> GetPendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        await _orders.ExpireReservationsAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        DateTime soon = now.AddHours(24);

        List<PendingOrderRow> rows = await _context.PedidosCliente
            .AsNoTracking()
            .Where(order => PendingStates.Contains(order.EstadoPedido.EPE_Codigo))
            .Select(order => new PendingOrderRow
            {
                Id = order.PED_PedidoCliente,
                Number = order.PED_Numero,
                Customer = order.Cliente.Persona.PersonaJuridica != null
                    ? order.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                    : order.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + order.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                StatusCode = order.EstadoPedido.EPE_Codigo,
                Status = order.EstadoPedido.EPE_Nombre,
                SaleType = order.TipoVenta != null ? order.TipoVenta.TVE_Nombre : "Contado",
                Date = order.PED_Fecha,
                ReservationExpiresAt = order.PED_FechaVencimientoReserva,
                ExpiresSoon = order.EstadoPedido.EPE_Codigo == OrderStatus.Reserved &&
                              order.PED_FechaVencimientoReserva != null && order.PED_FechaVencimientoReserva <= soon,
                Total = order.PED_Total,
                Employee = order.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           order.Empleado.Persona.PersonaNatural.PNA_PrimerApellido
            })
            .ToListAsync(cancellationToken);

        foreach (PendingOrderRow row in rows)
        {
            row.AgeDays = BusinessTime.Today.DayNumber - BusinessTime.ToLocalDate(row.Date).DayNumber;
        }

        // Primero lo que espera una decisión, luego lo reservado por vencer.
        rows = rows
            .OrderBy(row => Array.IndexOf(PendingStates, row.StatusCode))
            .ThenBy(row => row.ReservationExpiresAt ?? DateTime.MaxValue)
            .ThenBy(row => row.Date)
            .ToList();

        return new PendingOrdersResponse
        {
            GeneratedAt = now,
            ExpiringSoon = rows.Count(row => row.ExpiresSoon),
            Summary = rows
                .GroupBy(row => new { row.StatusCode, row.Status })
                .OrderBy(group => Array.IndexOf(PendingStates, group.Key.StatusCode))
                .Select(group => new PendingOrdersSummary
                {
                    StatusCode = group.Key.StatusCode,
                    Status = group.Key.Status,
                    Count = group.Count(),
                    Total = group.Sum(row => row.Total)
                })
                .ToList(),
            Rows = rows
        };
    }
}
