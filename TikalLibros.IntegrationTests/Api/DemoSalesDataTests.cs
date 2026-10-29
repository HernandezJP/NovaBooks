using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Datos de demostración de pedidos, ventas y caja.</summary>
public sealed class DemoSalesDataTests : ApiTestBase
{
    public DemoSalesDataTests(TikalLibrosApiFactory factory)
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
    }

    [Fact]
    public async Task Demostracion_CargaPedidosVentasYCajaSinDuplicarAlRepetir()
    {
        await SeedAsync();
        await SeedAsync();

        var summary = await Factory.QueryAsync(async context => new
        {
            Customers = await context.Clientes.CountAsync(),
            Orders = await context.PedidosCliente
                .GroupBy(order => order.EstadoPedido.EPE_Codigo)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count),
            Sales = await context.Ventas
                .GroupBy(sale => sale.EstadoVenta.EVE_Codigo)
                .Select(group => new { group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.Key, item => item.Count),
            OpenSessions = await context.AperturasCaja.CountAsync(item => item.ACA_Abierta),
            Closings = await context.CierresCaja.CountAsync(),
            ShortClosings = await context.CierresCaja.CountAsync(item => item.CCA_Diferencia < 0),
            Users = await context.Users.CountAsync(user =>
                user.UserName == DemoSalesSeeder.CashierUserName || user.UserName == DemoSalesSeeder.SupervisorUserName),
            Reserved = await context.Existencias.SumAsync(item => item.EXI_CantidadReservada),
            NegativeStock = await context.Existencias.CountAsync(item => item.EXI_CantidadDisponible < 0),
            StockUnits = await context.Existencias.SumAsync(item => item.EXI_CantidadDisponible),
            LotUnits = await context.Lotes.SumAsync(item => item.LOT_CantidadActual)
        });

        Assert.Equal(8, summary.Customers);
        Assert.Equal(2, summary.Users);

        Assert.Equal(1, summary.Orders[OrderStatus.PendingApproval]);
        Assert.Equal(1, summary.Orders[OrderStatus.Reserved]);
        Assert.Equal(3, summary.Orders[OrderStatus.Invoiced]); // contado, aprobado y al crédito
        Assert.Equal(1, summary.Orders[OrderStatus.Rejected]);
        Assert.Equal(1, summary.Orders[OrderStatus.Cancelled]);
        Assert.Equal(1, summary.Orders[OrderStatus.Draft]);

        Assert.Equal(1, summary.Sales[SaleStatus.Annulled]);
        Assert.Equal(2, summary.Sales[SaleStatus.Partial]); // pedido al crédito y venta al crédito con abono
        Assert.True(summary.Sales[SaleStatus.Paid] >= 6);

        Assert.Equal(1, summary.OpenSessions); // turno de la tarde del cajero
        Assert.Equal(2, summary.Closings);
        Assert.Equal(1, summary.ShortClosings);

        Assert.True(summary.Reserved > 0);
        Assert.Equal(0, summary.NegativeStock);
        Assert.Equal(summary.StockUnits, summary.LotUnits);
    }
}
