using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Casos borde entre pedidos, existencia y ventas.</summary>
public sealed class SalesEdgeCasesTests : SalesTestBase
{
    public SalesEdgeCasesTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Aprobar_SiLaExistenciaSeVendioMientrasEsperaba_Devuelve409YSiguePorAprobar()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient supervisor, _) = await EmployeeClientAsync(OrderPermissions.View, OrderPermissions.Approve);
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m, stock: 3);

        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 3, 20m)));
        Assert.Equal(OrderStatus.PendingApproval, (await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id))).StatusCode);

        // Por aprobar no reserva: una venta de mostrador se lleva 2 de las 3.
        await OkAsync<SaleResponse>(await SellAsync(admin, Sale(null, [await CashAsync(admin, 200m)], (book, 2, 0m))), HttpStatusCode.Created);

        HttpResponseMessage approve = await supervisor.PostAsJsonAsync($"api/pedidos/{order.Id}/aprobar", new ApproveOrderRequest());

        Assert.Equal(HttpStatusCode.Conflict, approve.StatusCode);
        Assert.Contains("se piden 3 y hay 1", await ReadProblemDetailAsync(approve));
        Assert.Equal(OrderStatus.PendingApproval, (await admin.GetFromJsonAsync<OrderResponse>($"api/pedidos/{order.Id}"))!.StatusCode);
    }

    [Fact]
    public async Task Facturar_PedidoConReservaVencida_Devuelve409YNoVende()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 70m, stock: 2);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 2, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        await Factory.QueryAsync(async context =>
        {
            var stored = await context.PedidosCliente.SingleAsync(item => item.PED_PedidoCliente == order.Id);
            stored.PED_Fecha = DateTime.UtcNow.AddDays(-5);
            stored.PED_FechaVencimientoReserva = DateTime.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
            return true;
        });

        HttpResponseMessage invoice = await admin.PostAsJsonAsync(
            $"api/ventas/desde-pedido/{order.Id}", new InvoiceOrderRequest { Payments = [await CashAsync(admin, 140m)] });

        Assert.Equal(HttpStatusCode.Conflict, invoice.StatusCode);
        Assert.Contains("vencido", await ReadProblemDetailAsync(invoice));
        Assert.Equal((2m, 0m), ((await StockAsync(admin, book)).Available, (await StockAsync(admin, book)).Reserved));
    }

    [Fact]
    public async Task VentaDeMostrador_NoPuedeTomarLoReservadoPorUnPedido()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 50m, stock: 2);
        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 2, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));

        HttpResponseMessage sale = await SellAsync(admin, Sale(null, [await CashAsync(admin, 50m)], (book, 1, 0m)));

        Assert.Equal(HttpStatusCode.Conflict, sale.StatusCode);
        Assert.Contains("se piden 1 y hay 0", await ReadProblemDetailAsync(sale));
    }
}
