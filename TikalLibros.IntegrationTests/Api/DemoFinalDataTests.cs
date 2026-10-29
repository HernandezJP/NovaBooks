using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.Infrastructure.Services.Inventory;
using TikalLibros.Infrastructure.Services.Purchasing;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Datos de demostración de la entrega final sobre la demo de ventas.</summary>
public sealed class DemoFinalDataTests : ApiTestBase
{
    public DemoFinalDataTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private async Task SeedAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DemoCatalogSeeder.SeedAsync(context, Path.Combine(Factory.ImagesPath, "demo"));
        await scope.ServiceProvider.GetRequiredService<DemoOperationsSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<DemoSalesSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<DemoFinalSeeder>().SeedAsync();
    }

    [Fact]
    public async Task Demostracion_CargaSucursalTrasladosDevolucionesEHistorialSinDuplicar()
    {
        await SeedAsync();
        await SeedAsync();

        DateTime monthStart = BusinessTime.StartOfDayUtc(new DateOnly(BusinessTime.Today.Year, BusinessTime.Today.Month, 1));

        var summary = await Factory.QueryAsync(async context => new
        {
            Branches = await context.Sucursales.CountAsync(item => item.SUC_Nombre == DemoFinalSeeder.BranchName),
            Warehouses = await context.Almacenes.CountAsync(item => item.ALM_Nombre == DemoFinalSeeder.WarehouseName && item.ALM_EsPrincipal),
            Registers = await context.Cajas.CountAsync(item => item.CAJ_Nombre == DemoFinalSeeder.CashRegisterName),
            Transfers = await context.MovimientosInventario.CountAsync(item => item.TipoMovimiento.TMI_Codigo == TransferService.TransferType),
            AnnulledTransfers = await context.MovimientosInventario.CountAsync(item => item.TipoMovimiento.TMI_Codigo == TransferService.TransferType && item.MOV_Anulado),
            Returns = await context.DevolucionesVenta.CountAsync(),
            CreditNotes = await context.DevolucionesPagos.CountAsync(item => item.MetodoPago.MPA_Codigo == "CREDITO"),
            Scrapped = await context.DevolucionesVentaDetalles.CountAsync(item => !item.DVD_ReintegraInventario),
            SupplierReturns = await context.MovimientosInventario.CountAsync(item => item.TipoMovimiento.TMI_Codigo == SupplierReturnService.SupplierReturnType),
            HistorySales = await context.Ventas.CountAsync(item => item.VEN_Fecha < monthStart),
            XelaUnits = await context.Existencias.Where(item => item.Almacen.ALM_Nombre == DemoFinalSeeder.WarehouseName).SumAsync(item => item.EXI_CantidadDisponible),
            NegativeStock = await context.Existencias.CountAsync(item => item.EXI_CantidadDisponible < 0),
            StockUnits = await context.Existencias.SumAsync(item => item.EXI_CantidadDisponible),
            LotUnits = await context.Lotes.SumAsync(item => item.LOT_CantidadActual)
        });

        Assert.Equal((1, 1, 1), (summary.Branches, summary.Warehouses, summary.Registers));
        Assert.Equal((3, 1), (summary.Transfers, summary.AnnulledTransfers));
        Assert.Equal(14m, summary.XelaUnits); // 3 libros × 4 + 2 de reposición; el anulado regresó
        Assert.Equal(3, summary.Returns);
        Assert.Equal(1, summary.CreditNotes);
        Assert.Equal(1, summary.Scrapped);
        Assert.Equal(1, summary.SupplierReturns);
        Assert.Equal(10, summary.HistorySales);

        Assert.Equal(0, summary.NegativeStock);
        Assert.Equal(summary.StockUnits, summary.LotUnits);

        // Las situaciones de la demo aparecen en el centro de alertas.
        HttpClient admin = await AdminAsync();
        AlertsResponse alerts = (await admin.GetFromJsonAsync<AlertsResponse>("api/alertas"))!;
        Assert.Contains(alerts.Items, item => item.Code == "CREDITOS_VENCIDOS");
        Assert.Contains(alerts.Items, item => item.Code == "CAJAS_SIN_CERRAR");

        DashboardResponse dashboard = (await admin.GetFromJsonAsync<DashboardResponse>("api/dashboard"))!;
        Assert.True(dashboard.Sales!.Last12Months.Count(month => month.Total > 0) >= 6);
    }
}
