using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Reportes de gestión: ventas netas de devoluciones, compras, caja,
/// rentabilidad y cuentas por cobrar. La base se comparte entre las pruebas
/// de la clase, así que los totales se comparan por diferencia.
/// </summary>
public sealed class ManagementReportsTests : SalesTestBase
{
    public ManagementReportsTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string uri) => (await client.GetFromJsonAsync<T>(uri))!;

    private static async Task<ReturnResponse> ReturnAsync(HttpClient client, SaleResponse sale, int book, int quantity)
    {
        int reason = (await GetAsync<ReturnCatalogsResponse>(client, "api/devoluciones/catalogos")).Reasons.Single(item => item.Code == "INCORRECTO").Id;

        return await OkAsync<ReturnResponse>(await client.PostAsJsonAsync("api/devoluciones", new SaveReturnRequest
        {
            SaleId = sale.Id,
            Lines = [new ReturnLineRequest { SaleLineId = sale.Lines.Single(line => line.BookId == book).LineId, Quantity = quantity, ReasonId = reason, Notes = "Título equivocado" }]
        }), HttpStatusCode.Created);
    }

    [Fact]
    public async Task Ventas_DescuentaDevolucionesYNoCuentaElAbonoComoCobro()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin, creditLimit: 5000m, creditDays: 30);
        int book = await CreateBookAsync(admin, price: 100m, stock: 5);

        SalesReportResponse before = await GetAsync<SalesReportResponse>(admin, "api/reportes/ventas");

        DirectSaleRequest request = Sale(customer, [], (book, 4, 0m));
        request.SaleType = SaleType.Credit;
        SaleResponse sale = await OkAsync<SaleResponse>(await SellAsync(admin, request), HttpStatusCode.Created);
        await ReturnAsync(admin, sale, book, 1);

        SalesReportResponse after = await GetAsync<SalesReportResponse>(admin, "api/reportes/ventas");

        Assert.Equal(400m, after.Total - before.Total);
        Assert.Equal((1, 100m), (after.ReturnsCount - before.ReturnsCount, after.Returned - before.Returned));
        Assert.Equal(after.Total - after.Returned, after.NetTotal);
        // El abono de la devolución al saldo no es dinero cobrado.
        Assert.Equal(0m, after.Collected - before.Collected);
        Assert.Equal(300m, after.Pending - before.Pending);

        TopBooksResponse top = await GetAsync<TopBooksResponse>(admin, "api/reportes/libros-mas-vendidos?top=100");
        TopBookRow row = top.Rows.Single(item => item.BookId == book);
        Assert.Equal((3m, 300m), (row.Units, row.Revenue));
    }

    [Fact]
    public async Task Rentabilidad_IngresoSinIvaCostoYMargenNetosDeDevoluciones()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 112m);
        await AddStockAsync(admin, book, 3, cost: 50m);

        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 224m)], (book, 2, 0m))), HttpStatusCode.Created);
        await ReturnAsync(admin, sale, book, 1);

        ProfitabilityReportResponse report = await GetAsync<ProfitabilityReportResponse>(admin, "api/reportes/rentabilidad");
        ProfitRow row = report.ByBook.Single(item => item.Id == book);

        // Q112 con IVA = Q100 netos; costo Q50.
        Assert.Equal((1m, 100m, 50m, 50m, 50m), (row.Units, row.Revenue, row.Cost, row.Margin, row.MarginPercent!.Value));
        Assert.Contains(report.ByCategory, item => item.Name == "Sin categoría");
        Assert.True(report.ReturnedUnits >= 1);
        Assert.Equal(report.Revenue - report.Cost, report.Margin);
    }

    [Fact]
    public async Task Compras_RecepcionesMenosDevolucionesAlProveedor()
    {
        HttpClient admin = await AdminAsync();
        PurchasesReportResponse before = await GetAsync<PurchasesReportResponse>(admin, "api/reportes/compras");

        int book = await CreateBookAsync(admin);
        await AddStockAsync(admin, book, 10, cost: 40m);

        var line = await Factory.QueryAsync(context => context.RecepcionesCompraDetalles
            .Where(item => item.RCD_LibroId == book)
            .Select(item => new { item.RCD_RecepcionCompraId, item.RCD_RecepcionCompraDetalle })
            .SingleAsync());

        await OkAsync<SupplierReturnResponse>(await admin.PostAsJsonAsync("api/devoluciones-proveedor", new SaveSupplierReturnRequest
        {
            ReceptionId = line.RCD_RecepcionCompraId,
            Reason = "Ejemplares dañados",
            Lines = [new SupplierReturnLineRequest { ReceptionLineId = line.RCD_RecepcionCompraDetalle, Quantity = 2 }]
        }), HttpStatusCode.Created);

        PurchasesReportResponse after = await GetAsync<PurchasesReportResponse>(admin, "api/reportes/compras");

        Assert.Equal((1, 10m, 400m), (after.ReceptionsCount - before.ReceptionsCount, after.Units - before.Units, after.Total - before.Total));
        Assert.Equal((1, 80m), (after.SupplierReturnsCount - before.SupplierReturnsCount, after.SupplierReturnsTotal - before.SupplierReturnsTotal));
        Assert.Equal(after.Total - after.SupplierReturnsTotal, after.NetTotal);
        Assert.Contains(after.TopBooks, item => item.BookId == book && item.Total == 400m);
        Assert.Equal(after.Total, after.ByDay.Sum(day => day.Total));
    }

    [Fact]
    public async Task Caja_IncluyeLaAperturaConSusMovimientosYMetodos()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync(
            SalesPermissions.View, SalesPermissions.Create, CashPermissions.Open, CashPermissions.View, CatalogPermissions.View, CustomerPermissions.View);
        int sessionId = (await EnsureCashAsync(cashier)).Id;
        int book = await CreateBookAsync(admin, price: 60m, stock: 3);

        await OkAsync<SaleResponse>(await SellAsync(cashier, Sale(null, [await CashAsync(cashier, 60m)], (book, 1, 0m))), HttpStatusCode.Created);
        await OkAsync<SaleResponse>(await SellAsync(cashier, Sale(null, [await CardAsync(cashier, 120m)], (book, 2, 0m))), HttpStatusCode.Created);

        CashReportResponse report = await GetAsync<CashReportResponse>(admin, "api/reportes/caja");

        CashSessionReportRow row = report.Sessions.Single(item => item.Id == sessionId);
        Assert.True(row.IsOpen);
        Assert.Equal(100m, row.InitialAmount);
        Assert.Contains(report.ByType, item => item.Nature > 0 && item.Total >= 180m);
        Assert.Contains(report.ByMethod, item => item.Name.Contains("Tarjeta") && item.Total >= 120m);
        Assert.True(report.Income >= 180m);
    }

    [Fact]
    public async Task CuentasPorCobrar_AntiguedadPorVencimientoYFiltroDeVencidas()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin, creditLimit: 3000m, creditDays: 15);
        int book = await CreateBookAsync(admin, price: 250m, stock: 2);

        DirectSaleRequest request = Sale(customer, [await CashAsync(admin, 100m)], (book, 2, 0m));
        request.SaleType = SaleType.Credit;
        SaleResponse sale = await OkAsync<SaleResponse>(await SellAsync(admin, request), HttpStatusCode.Created);

        await Factory.QueryAsync(async context =>
        {
            var entity = await context.Ventas.SingleAsync(item => item.VEN_Venta == sale.Id);
            entity.VEN_FechaVencimientoCredito = BusinessTime.Today.AddDays(-40);
            return await context.SaveChangesAsync();
        });

        ReceivablesResponse report = await GetAsync<ReceivablesResponse>(admin, $"api/reportes/cuentas-por-cobrar?customerId={customer}");

        ReceivableSaleRow row = Assert.Single(report.Sales);
        Assert.Equal((400m, 40, AgingBuckets.Days60), (row.Balance, row.DaysOverdue, row.Bucket));
        Assert.Equal((400m, 400m), (report.Total, report.Overdue));
        Assert.Equal(400m, report.Buckets.Single(bucket => bucket.Name == AgingBuckets.Days60).Total);
        ReceivableCustomerRow customerRow = Assert.Single(report.Customers);
        Assert.Equal((3000m, 40), (customerRow.CreditLimit, customerRow.MaxDaysOverdue));

        ReceivablesResponse overdue = await GetAsync<ReceivablesResponse>(admin, "api/reportes/cuentas-por-cobrar?onlyOverdue=true");
        Assert.All(overdue.Sales, item => Assert.True(item.DaysOverdue > 0));
        Assert.Contains(overdue.Sales, item => item.SaleId == sale.Id);
    }

    [Fact]
    public async Task Permisos_CadaReporteExigeSuPermiso()
    {
        (HttpClient sales, _) = await EmployeeClientAsync(ReportPermissions.Sales);

        Assert.Equal(HttpStatusCode.OK, (await sales.GetAsync("api/reportes/cuentas-por-cobrar")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sales.GetAsync("api/reportes/compras")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sales.GetAsync("api/reportes/caja")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sales.GetAsync("api/reportes/rentabilidad")).StatusCode);

        (HttpClient management, _) = await EmployeeClientAsync(ReportPermissions.Purchases, ReportPermissions.Cash, ReportPermissions.Profitability);
        Assert.Equal(HttpStatusCode.OK, (await management.GetAsync("api/reportes/compras")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await management.GetAsync("api/reportes/caja")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await management.GetAsync("api/reportes/rentabilidad")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await management.GetAsync("api/reportes/caja?from=2026-10-10&to=2026-10-01")).StatusCode);
    }

}
