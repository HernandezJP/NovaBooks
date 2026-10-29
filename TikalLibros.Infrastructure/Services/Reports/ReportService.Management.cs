using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Domain.Entities.Sales;
using TikalLibros.Infrastructure.Services.Purchasing;

namespace TikalLibros.Infrastructure.Services.Reports;

/// <summary>Reportes de gestión: compras, caja, rentabilidad y cuentas por cobrar.</summary>
public sealed partial class ReportService
{
    private const int TopRows = 50;
    private const string NoCategory = "Sin categoría";
    private const string FinalConsumer = "Consumidor final";

    private static IReadOnlyCollection<SalesByDayRow> EveryDay(DateOnly from, DateOnly to, IEnumerable<(DateOnly Day, decimal Total)> rows)
    {
        Dictionary<DateOnly, (int Count, decimal Total)> byDay = rows
            .GroupBy(row => row.Day)
            .ToDictionary(group => group.Key, group => (group.Count(), group.Sum(row => row.Total)));

        return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(offset => from.AddDays(offset))
            .Select(day => new SalesByDayRow
            {
                Date = day,
                Count = byDay.TryGetValue(day, out var value) ? value.Count : 0,
                Total = byDay.TryGetValue(day, out value) ? value.Total : 0
            })
            .ToList();
    }

    private static decimal? Percent(decimal part, decimal whole) =>
        whole > 0 ? decimal.Round(part / whole * 100m, 2, MidpointRounding.AwayFromZero) : null;

    // ───────────────────────────── Compras ─────────────────────────────

    public async Task<OperationResult<PurchasesReportResponse>> GetPurchasesAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        OperationResult<(DateOnly From, DateOnly To)> period = ResolvePeriod(query);

        if (!period.Succeeded)
        {
            return period.As<PurchasesReportResponse>();
        }

        (DateOnly from, DateOnly to) = period.Data;
        DateTime start = BusinessTime.StartOfDayUtc(from);
        DateTime end = BusinessTime.EndOfDayUtcExclusive(to);

        var receptions = await _context.RecepcionesCompra
            .AsNoTracking()
            .Where(item => item.REC_FechaRecepcion >= start && item.REC_FechaRecepcion < end &&
                           (query.WarehouseId == null || item.REC_AlmacenId == query.WarehouseId))
            .Select(item => new
            {
                item.REC_RecepcionCompra,
                item.REC_FechaRecepcion,
                Annulled = item.EstadoRecepcionCompra.ERC_Codigo == ReceptionStatus.Annulled,
                Supplier = item.OrdenCompra.Proveedor.Persona.PersonaJuridica != null
                    ? item.OrdenCompra.Proveedor.Persona.PersonaJuridica.PJU_RazonSocial
                    : item.OrdenCompra.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.OrdenCompra.Proveedor.Persona.PersonaNatural.PNA_PrimerApellido
            })
            .ToListAsync(cancellationToken);

        int[] valid = receptions.Where(item => !item.Annulled).Select(item => item.REC_RecepcionCompra).ToArray();

        var lines = await _context.RecepcionesCompraDetalles
            .AsNoTracking()
            .Where(line => valid.Contains(line.RCD_RecepcionCompraId))
            .Select(line => new
            {
                line.RCD_RecepcionCompraId,
                line.RCD_LibroId,
                line.Libro.LIB_Codigo,
                line.Libro.LIB_Titulo,
                line.RCD_CantidadAceptada,
                line.RCD_CantidadRechazada,
                Total = line.RCD_CantidadAceptada * line.RCD_CostoUnitario
            })
            .ToListAsync(cancellationToken);

        var supplierReturns = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(item => item.TipoMovimiento.TMI_Codigo == SupplierReturnService.SupplierReturnType && !item.MOV_Anulado &&
                           item.MOV_Fecha >= start && item.MOV_Fecha < end &&
                           (query.WarehouseId == null || item.MOV_AlmacenOrigenId == query.WarehouseId))
            .Select(item => item.Detalles.Sum(line => line.MDE_CostoTotal))
            .ToListAsync(cancellationToken);

        // Órdenes vigentes con mercadería por recibir, valoradas a su costo por unidad.
        var pending = await _context.OrdenesCompra
            .AsNoTracking()
            .Where(order => order.EstadoOrdenCompra.EOC_Codigo == PurchaseOrderStatus.Issued ||
                            order.EstadoOrdenCompra.EOC_Codigo == PurchaseOrderStatus.Partial)
            .Select(order => new
            {
                order.OCO_OrdenCompra,
                Remaining = order.Detalles
                    .Where(line => line.OCD_CantidadSolicitada > line.OCD_CantidadRecibida)
                    .Sum(line => (line.OCD_CantidadSolicitada - line.OCD_CantidadRecibida) * line.OCD_Total / line.OCD_CantidadSolicitada)
            })
            .ToListAsync(cancellationToken);

        Dictionary<int, (DateOnly Day, string Supplier)> byReception = receptions
            .Where(item => !item.Annulled)
            .ToDictionary(item => item.REC_RecepcionCompra, item => (BusinessTime.ToLocalDate(item.REC_FechaRecepcion), item.Supplier));

        decimal total = LineAmounts.Round(lines.Sum(line => line.Total));
        decimal returned = supplierReturns.Sum();

        return OperationResult<PurchasesReportResponse>.Success(new PurchasesReportResponse
        {
            From = from,
            To = to,
            ReceptionsCount = valid.Length,
            AnnulledCount = receptions.Count - valid.Length,
            Units = lines.Sum(line => line.RCD_CantidadAceptada),
            RejectedUnits = lines.Sum(line => line.RCD_CantidadRechazada),
            Total = total,
            SupplierReturnsCount = supplierReturns.Count,
            SupplierReturnsTotal = returned,
            NetTotal = total - returned,
            PendingOrdersCount = pending.Count(order => order.Remaining > 0),
            PendingOrdersAmount = LineAmounts.Round(pending.Sum(order => order.Remaining)),
            ByDay = EveryDay(from, to, lines
                .GroupBy(line => line.RCD_RecepcionCompraId)
                .Select(group => (byReception[group.Key].Day, LineAmounts.Round(group.Sum(line => line.Total))))),
            BySupplier = lines
                .GroupBy(line => byReception[line.RCD_RecepcionCompraId].Supplier)
                .Select(group => new SalesByGroupRow
                {
                    Name = group.Key,
                    Count = group.Select(line => line.RCD_RecepcionCompraId).Distinct().Count(),
                    Total = LineAmounts.Round(group.Sum(line => line.Total))
                })
                .OrderByDescending(row => row.Total)
                .ToList(),
            TopBooks = lines
                .GroupBy(line => new { line.RCD_LibroId, line.LIB_Codigo, line.LIB_Titulo })
                .Select(group => new PurchasedBookRow
                {
                    BookId = group.Key.RCD_LibroId,
                    BookCode = group.Key.LIB_Codigo,
                    Title = group.Key.LIB_Titulo,
                    Units = group.Sum(line => line.RCD_CantidadAceptada),
                    Total = LineAmounts.Round(group.Sum(line => line.Total))
                })
                .OrderByDescending(row => row.Total)
                .Take(TopRows)
                .ToList()
        });
    }

    // ───────────────────────────── Caja ─────────────────────────────

    public async Task<OperationResult<CashReportResponse>> GetCashAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        OperationResult<(DateOnly From, DateOnly To)> period = ResolvePeriod(query);

        if (!period.Succeeded)
        {
            return period.As<CashReportResponse>();
        }

        (DateOnly from, DateOnly to) = period.Data;
        DateTime start = BusinessTime.StartOfDayUtc(from);
        DateTime end = BusinessTime.EndOfDayUtcExclusive(to);

        List<CashSessionReportRow> sessions = await _context.AperturasCaja
            .AsNoTracking()
            .Where(session => session.ACA_FechaApertura >= start && session.ACA_FechaApertura < end)
            .OrderByDescending(session => session.ACA_FechaApertura)
            .Select(session => new CashSessionReportRow
            {
                Id = session.ACA_AperturaCaja,
                CashRegister = session.Caja.CAJ_Nombre,
                Branch = session.Caja.Sucursal.SUC_Nombre,
                Employee = session.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           session.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                OpenedAt = session.ACA_FechaApertura,
                ClosedAt = session.CierreCaja != null ? session.CierreCaja.CCA_FechaCierre : null,
                IsOpen = session.ACA_Abierta,
                InitialAmount = session.ACA_MontoInicial,
                Expected = session.CierreCaja != null ? session.CierreCaja.CCA_MontoEsperado : null,
                Counted = session.CierreCaja != null ? session.CierreCaja.CCA_MontoContado : null,
                Difference = session.CierreCaja != null ? session.CierreCaja.CCA_Diferencia : null
            })
            .ToListAsync(cancellationToken);

        int[] ids = sessions.Select(session => session.Id).ToArray();

        var movements = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(movement => ids.Contains(movement.MCA_AperturaCajaId) && !movement.MCA_Anulado)
            .GroupBy(movement => new
            {
                Type = movement.TipoMovimientoCaja.TMC_Nombre,
                movement.TipoMovimientoCaja.TMC_Naturaleza,
                Method = movement.MetodoPago.MPA_Nombre
            })
            .Select(group => new { group.Key.Type, group.Key.TMC_Naturaleza, group.Key.Method, Count = group.Count(), Total = group.Sum(movement => movement.MCA_MontoBase) })
            .ToListAsync(cancellationToken);

        return OperationResult<CashReportResponse>.Success(new CashReportResponse
        {
            From = from,
            To = to,
            SessionsCount = sessions.Count,
            OpenSessions = sessions.Count(session => session.IsOpen),
            Income = movements.Where(row => row.TMC_Naturaleza > 0).Sum(row => row.Total),
            Expense = movements.Where(row => row.TMC_Naturaleza < 0).Sum(row => row.Total),
            Shortages = sessions.Where(session => session.Difference < 0).Sum(session => session.Difference!.Value),
            Overages = sessions.Where(session => session.Difference > 0).Sum(session => session.Difference!.Value),
            ByType = movements
                .GroupBy(row => new { row.Type, row.TMC_Naturaleza })
                .Select(group => new CashTypeRow { Name = group.Key.Type, Nature = group.Key.TMC_Naturaleza, Count = group.Sum(row => row.Count), Total = group.Sum(row => row.Total) })
                .OrderByDescending(row => row.Nature)
                .ThenByDescending(row => row.Total)
                .ToList(),
            ByMethod = movements
                .GroupBy(row => row.Method)
                .Select(group => new SalesByGroupRow
                {
                    Name = group.Key,
                    Count = group.Sum(row => row.Count),
                    Total = group.Sum(row => row.TMC_Naturaleza * row.Total)
                })
                .OrderByDescending(row => row.Total)
                .ToList(),
            Sessions = sessions
        });
    }

    // ───────────────────────────── Rentabilidad ─────────────────────────────

    public async Task<OperationResult<ProfitabilityReportResponse>> GetProfitabilityAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        OperationResult<(DateOnly From, DateOnly To)> period = ResolvePeriod(query);

        if (!period.Succeeded)
        {
            return period.As<ProfitabilityReportResponse>();
        }

        (DateOnly from, DateOnly to) = period.Data;
        IQueryable<PB_VENTA> sales = SalesInPeriod(from, to, query.WarehouseId).Where(sale => !sale.VEN_Anulada);

        var lines = await _context.VentasDetalles
            .AsNoTracking()
            .Where(line => sales.Any(sale => sale.VEN_Venta == line.VDE_VentaId))
            .Select(line => new
            {
                line.VDE_LibroId,
                line.Libro.LIB_Codigo,
                line.Libro.LIB_Titulo,
                Employee = line.Venta.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           line.Venta.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                line.VDE_Cantidad,
                line.VDE_CantidadDevuelta,
                line.VDE_Total,
                line.VDE_MontoImpuesto,
                line.VDE_CostoUnitario
            })
            .ToListAsync(cancellationToken);

        int[] books = lines.Select(line => line.VDE_LibroId).Distinct().ToArray();

        // Categoría principal del libro (o la primera, si ninguna está marcada como principal).
        Dictionary<int, string> categories = (await _context.LibrosCategorias
                .AsNoTracking()
                .Where(item => books.Contains(item.LCA_LibroId))
                .Select(item => new { item.LCA_LibroId, item.LCA_Principal, item.Categoria.CAT_Nombre })
                .ToListAsync(cancellationToken))
            .GroupBy(item => item.LCA_LibroId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.LCA_Principal).First().CAT_Nombre);

        List<NetLine> net = lines
            .Select(line =>
            {
                decimal units = line.VDE_Cantidad - line.VDE_CantidadDevuelta;
                decimal share = line.VDE_Cantidad == 0 ? 0 : units / line.VDE_Cantidad;

                return new NetLine(
                    line.VDE_LibroId,
                    line.LIB_Codigo,
                    line.LIB_Titulo,
                    line.Employee,
                    categories.GetValueOrDefault(line.VDE_LibroId, NoCategory),
                    units,
                    line.VDE_CantidadDevuelta,
                    (line.VDE_Total - line.VDE_MontoImpuesto) * share,
                    units * line.VDE_CostoUnitario);
            })
            .ToList();

        decimal revenue = LineAmounts.Round(net.Sum(item => item.Revenue));
        decimal cost = LineAmounts.Round(net.Sum(item => item.Cost));

        return OperationResult<ProfitabilityReportResponse>.Success(new ProfitabilityReportResponse
        {
            From = from,
            To = to,
            Units = net.Sum(item => item.Units),
            ReturnedUnits = net.Sum(item => item.Returned),
            Revenue = revenue,
            Cost = cost,
            Margin = revenue - cost,
            MarginPercent = Percent(revenue - cost, revenue),
            ByCategory = net
                .GroupBy(item => item.Category)
                .Select(group => Row(group.Key, group))
                .OrderByDescending(row => row.Margin)
                .ToList(),
            ByBook = net
                .Where(item => item.Units > 0)
                .GroupBy(item => new { item.BookId, item.Code, item.Title })
                .Select(group => Row(group.Key.Title, group, group.Key.BookId, group.Key.Code))
                .OrderByDescending(row => row.Margin)
                .Take(TopRows)
                .ToList(),
            ByEmployee = net
                .GroupBy(item => item.Employee)
                .Select(group => Row(group.Key, group))
                .OrderByDescending(row => row.Margin)
                .ToList()
        });
    }

    private sealed record NetLine(
        int BookId,
        string Code,
        string Title,
        string Employee,
        string Category,
        decimal Units,
        decimal Returned,
        decimal Revenue,
        decimal Cost);

    private static ProfitRow Row(string name, IEnumerable<NetLine> group, int? id = null, string? code = null)
    {
        List<NetLine> items = group.ToList();
        decimal revenue = LineAmounts.Round(items.Sum(item => item.Revenue));
        decimal cost = LineAmounts.Round(items.Sum(item => item.Cost));

        return new ProfitRow
        {
            Id = id,
            Code = code,
            Name = name,
            Units = items.Sum(item => item.Units),
            Revenue = revenue,
            Cost = cost,
            Margin = revenue - cost,
            MarginPercent = Percent(revenue - cost, revenue)
        };
    }

    // ───────────────────────────── Cuentas por cobrar ─────────────────────────────

    public async Task<ReceivablesResponse> GetReceivablesAsync(
        ReceivablesQuery query,
        CancellationToken cancellationToken = default)
    {
        DateOnly today = BusinessTime.Today;

        var sales = await _context.Ventas
            .AsNoTracking()
            .Where(sale => !sale.VEN_Anulada && sale.VEN_TotalPagado < sale.VEN_Total &&
                           (query.CustomerId == null || sale.VEN_ClienteId == query.CustomerId))
            .Select(sale => new
            {
                sale.VEN_Venta,
                sale.VEN_Numero,
                sale.VEN_Fecha,
                sale.VEN_FechaVencimientoCredito,
                sale.VEN_ClienteId,
                Customer = sale.Cliente == null
                    ? FinalConsumer
                    : sale.Cliente.Persona.PersonaJuridica != null
                        ? sale.Cliente.Persona.PersonaJuridica.PJU_RazonSocial
                        : sale.Cliente.Persona.PersonaNatural!.PNA_PrimerNombre + " " + sale.Cliente.Persona.PersonaNatural.PNA_PrimerApellido,
                CreditLimit = sale.Cliente != null ? sale.Cliente.CLI_LimiteCredito : 0,
                sale.VEN_Total,
                sale.VEN_TotalPagado
            })
            .ToListAsync(cancellationToken);

        List<ReceivableSaleRow> rows = sales
            .Select(sale =>
            {
                DateOnly due = sale.VEN_FechaVencimientoCredito ?? BusinessTime.ToLocalDate(sale.VEN_Fecha);
                int overdue = Math.Max(0, today.DayNumber - due.DayNumber);

                return new ReceivableSaleRow
                {
                    SaleId = sale.VEN_Venta,
                    Number = sale.VEN_Numero,
                    Date = sale.VEN_Fecha,
                    DueDate = due,
                    CustomerId = sale.VEN_ClienteId,
                    Customer = sale.Customer,
                    Total = sale.VEN_Total,
                    Paid = sale.VEN_TotalPagado,
                    Balance = sale.VEN_Total - sale.VEN_TotalPagado,
                    DaysOverdue = overdue,
                    Bucket = AgingBuckets.For(overdue)
                };
            })
            .Where(row => !query.OnlyOverdue || row.DaysOverdue > 0)
            .OrderByDescending(row => row.DaysOverdue)
            .ThenByDescending(row => row.Balance)
            .ToList();

        Dictionary<int, decimal> limits = sales
            .Where(sale => sale.VEN_ClienteId != null)
            .GroupBy(sale => sale.VEN_ClienteId!.Value)
            .ToDictionary(group => group.Key, group => group.First().CreditLimit);

        return new ReceivablesResponse
        {
            AsOf = today,
            Total = rows.Sum(row => row.Balance),
            Overdue = rows.Where(row => row.DaysOverdue > 0).Sum(row => row.Balance),
            SalesCount = rows.Count,
            CustomersCount = rows.Select(row => row.CustomerId).Distinct().Count(),
            Buckets = AgingBuckets.All
                .Select(bucket => new SalesByGroupRow
                {
                    Name = bucket,
                    Count = rows.Count(row => row.Bucket == bucket),
                    Total = rows.Where(row => row.Bucket == bucket).Sum(row => row.Balance)
                })
                .ToList(),
            Customers = rows
                .GroupBy(row => new { row.CustomerId, row.Customer })
                .Select(group => new ReceivableCustomerRow
                {
                    CustomerId = group.Key.CustomerId,
                    Customer = group.Key.Customer,
                    CreditLimit = group.Key.CustomerId is int id ? limits.GetValueOrDefault(id) : 0,
                    Sales = group.Count(),
                    Balance = group.Sum(row => row.Balance),
                    Overdue = group.Where(row => row.DaysOverdue > 0).Sum(row => row.Balance),
                    MaxDaysOverdue = group.Max(row => row.DaysOverdue)
                })
                .OrderByDescending(row => row.Overdue)
                .ThenByDescending(row => row.Balance)
                .ToList(),
            Sales = rows
        };
    }
}
