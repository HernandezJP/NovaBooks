using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Configuration;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Services.Sales;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>Factura electrónica (certificación simulada) emitida con cada venta.</summary>
public sealed class InvoiceTests : SalesTestBase
{
    public InvoiceTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static Task<InvoiceResponse?> InvoiceAsync(HttpClient client, int sale) =>
        client.GetFromJsonAsync<InvoiceResponse>($"api/ventas/{sale}/factura");

    [Fact]
    public async Task Venta_EmiteFacturaConSerieNumeroYAutorizacion()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 112m, stock: 3);

        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 212.80m)], (book, 2, 5m))), HttpStatusCode.Created);

        InvoiceSummaryResponse summary = sale.Invoice!;
        Guid authorization = Guid.Parse(summary.Authorization);
        Assert.Equal(authorization.ToString("N")[..8].ToUpperInvariant(), summary.Series);
        Assert.Equal(Convert.ToInt64(authorization.ToString("N")[8..16], 16), summary.Number);
        Assert.True(summary.IsDemo);
        Assert.False(summary.Annulled);

        InvoiceResponse invoice = (await InvoiceAsync(admin, sale.Id))!;
        Assert.Equal((InvoiceService.FinalConsumerNit, InvoiceService.FinalConsumerName), (invoice.ReceiverNit, invoice.ReceiverName));
        Assert.Equal((212.80m, 22.80m), (invoice.Total, invoice.Tax));
        Assert.Equal("Doscientos doce quetzales con 80/100", invoice.TotalInWords);
        List<SystemParameterResponse> parameters = (await admin.GetFromJsonAsync<List<SystemParameterResponse>>("api/parametros"))!;
        Assert.Equal(parameters.Single(item => item.Key == SystemParameterKeys.InvoiceIssuerNit).Value, invoice.IssuerNit);
        Assert.Equal("Tikal Libros, S.A.", invoice.IssuerName);
        Assert.Contains("Guatemala", invoice.IssuerAddress);
        InvoiceLineResponse line = Assert.Single(invoice.Lines);
        Assert.Equal((2m, 112m, 11.20m, 212.80m), (line.Quantity, line.UnitPrice, line.Discount, line.Total));
    }

    [Fact]
    public async Task VentaConCliente_FacturaASuNombreYNit()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin);
        int book = await CreateBookAsync(admin, price: 50m, stock: 1);

        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(customer, [await CardAsync(admin, 50m)], (book, 1, 0m))), HttpStatusCode.Created);

        InvoiceResponse invoice = (await InvoiceAsync(admin, sale.Id))!;
        Assert.Equal(sale.Customer, invoice.ReceiverName);
        Assert.Equal(sale.CustomerNit, invoice.ReceiverNit);
        Assert.NotEqual(InvoiceService.FinalConsumerNit, invoice.ReceiverNit);
    }

    [Fact]
    public async Task VentaAlCreditoYPedidoFacturado_TambienEmitenFactura()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin, creditLimit: 1000m, creditDays: 30);
        int book = await CreateBookAsync(admin, price: 100m, stock: 5);

        DirectSaleRequest credit = Sale(customer, [], (book, 1, 0m));
        credit.SaleType = SaleType.Credit;
        SaleResponse creditSale = await OkAsync<SaleResponse>(await SellAsync(admin, credit), HttpStatusCode.Created);

        OrderResponse order = await CreateOrderAsync(admin, Order(customer, (book, 1, 0m)));
        await OkAsync<OrderResponse>(await ConfirmAsync(admin, order.Id));
        SaleResponse invoiced = await OkAsync<SaleResponse>(await admin.PostAsJsonAsync(
            $"api/ventas/desde-pedido/{order.Id}", new InvoiceOrderRequest { Payments = [await CashAsync(admin, 100m)] }), HttpStatusCode.Created);

        Assert.NotNull(creditSale.Invoice);
        Assert.NotNull(invoiced.Invoice);
        Assert.NotEqual(creditSale.Invoice!.Authorization, invoiced.Invoice!.Authorization);
        Assert.Equal("Crédito", (await InvoiceAsync(admin, creditSale.Id))!.SaleType);
    }

    [Fact]
    public async Task AnularVenta_AnulaSuFactura()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 40m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 40m)], (book, 1, 0m))), HttpStatusCode.Created);

        SaleResponse annulled = await OkAsync<SaleResponse>(await admin.PostAsJsonAsync(
            $"api/ventas/{sale.Id}/anular", new AnnulSaleRequest { Reason = "Se registró en la caja equivocada" }));

        Assert.True(annulled.Invoice!.Annulled);
        InvoiceResponse invoice = (await InvoiceAsync(admin, sale.Id))!;
        Assert.True(invoice.Annulled);
        Assert.Equal("Se registró en la caja equivocada", invoice.AnnulmentReason);
    }

    [Fact]
    public async Task Busqueda_PorSerieONumeroDeFactura()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 30m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 30m)], (book, 1, 0m))), HttpStatusCode.Created);

        foreach (string search in new[] { sale.Invoice!.Series, sale.Invoice.Number.ToString() })
        {
            PagedResponse<SaleListItemResponse> found = (await admin.GetFromJsonAsync<PagedResponse<SaleListItemResponse>>(
                $"api/ventas?search={search}"))!;
            Assert.Contains(found.Items, item => item.Id == sale.Id);
        }
    }

    [Fact]
    public async Task VentasSinFactura_SeFacturanAlArrancar()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 25m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 25m)], (book, 1, 0m))), HttpStatusCode.Created);

        await Factory.QueryAsync(async context =>
        {
            context.FacturasVenta.Remove(await context.FacturasVenta.SingleAsync(item => item.FAC_VentaId == sale.Id));
            await context.SaveChangesAsync();
            return true;
        });

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"api/ventas/{sale.Id}/factura")).StatusCode);

        using IServiceScope scope = Factory.Services.CreateScope();
        int issued = await scope.ServiceProvider.GetRequiredService<InvoiceService>().IssueMissingAsync();

        Assert.True(issued >= 1);
        Assert.NotNull(await InvoiceAsync(admin, sale.Id));
    }

    [Fact]
    public async Task NitDelEmisor_SeConfiguraYSeValida()
    {
        HttpClient admin = await AdminAsync();

        HttpResponseMessage invalid = await admin.PutAsJsonAsync(
            $"api/parametros/{SystemParameterKeys.InvoiceIssuerNit}", new UpdateSystemParameterRequest { Value = "12AB" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        SystemParameterResponse updated = await OkAsync<SystemParameterResponse>(await admin.PutAsJsonAsync(
            $"api/parametros/{SystemParameterKeys.InvoiceIssuerNit}", new UpdateSystemParameterRequest { Value = "1234567-k" }));
        Assert.Equal("1234567K", updated.Value);

        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 10m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 10m)], (book, 1, 0m))), HttpStatusCode.Created);

        Assert.Equal("1234567K", (await InvoiceAsync(admin, sale.Id))!.IssuerNit);
    }
}
