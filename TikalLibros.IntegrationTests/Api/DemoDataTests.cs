using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Datos de demostración de compras e inventario.</summary>
public sealed class DemoDataTests : ApiTestBase
{
    public DemoDataTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private async Task SeedAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DemoCatalogSeeder.SeedAsync(context, Path.Combine(Factory.ImagesPath, "demo"));
        await scope.ServiceProvider.GetRequiredService<DemoOperationsSeeder>().SeedAsync();
    }

    [Fact]
    public async Task Demostracion_CargaComprasEInventarioSinDuplicarAlRepetir()
    {
        await SeedAsync();
        await SeedAsync();

        var summary = await Factory.QueryAsync(async context => new
        {
            Suppliers = await context.Proveedores.CountAsync(item => item.PRO_Codigo.StartsWith("PRO-")),
            Employees = await context.Empleados.CountAsync(),
            Orders = await context.OrdenesCompra
                .GroupBy(order => order.EstadoOrdenCompra.EOC_Codigo)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count),
            Receptions = await context.RecepcionesCompra.CountAsync(),
            Lots = await context.Lotes.CountAsync(),
            Movements = await context.MovimientosInventario.CountAsync(),
            Adjustments = await context.AjustesInventario.CountAsync(),
            OutOfStock = await context.Existencias.CountAsync(item => item.EXI_CantidadDisponible == 0),
            BelowMinimum = await context.Existencias.CountAsync(item =>
                item.EXI_CantidadDisponible > 0 && item.EXI_CantidadDisponible <= item.EXI_StockMinimo),
            StockUnits = await context.Existencias.SumAsync(item => item.EXI_CantidadDisponible),
            LotUnits = await context.Lotes.SumAsync(item => item.LOT_CantidadActual)
        });

        Assert.Equal(5, summary.Suppliers);
        Assert.Equal(5, summary.Employees); // 4 de demostración + el del administrador
        Assert.Equal(1, summary.Orders["BORRADOR"]);
        Assert.Equal(1, summary.Orders["EMITIDA"]);
        Assert.Equal(1, summary.Orders["PARCIAL"]);
        Assert.Equal(2, summary.Orders["RECIBIDA"]); // incluye la compra directa
        Assert.Equal(1, summary.Orders["CANCELADA"]);
        Assert.Equal(3, summary.Receptions);
        Assert.Equal(2, summary.Adjustments);
        Assert.Equal(5, summary.Movements); // 3 entradas por compra + 2 ajustes
        Assert.True(summary.OutOfStock > 0);
        Assert.True(summary.BelowMinimum > 0);
        Assert.True(summary.Lots > 0);

        // Las existencias coinciden con lo que queda en los lotes.
        Assert.Equal(summary.StockUnits, summary.LotUnits);
    }
}
