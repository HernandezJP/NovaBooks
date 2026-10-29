using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Devoluciones de clientes: monto proporcional, abono al saldo o reembolso
/// por caja, reingreso al inventario según el motivo, plazo y anulación.
/// </summary>
public sealed class ReturnsTests : SalesTestBase
{
    private static readonly string[] Cashier =
    [
        SalesPermissions.View, SalesPermissions.Create, SalesPermissions.Return,
        CashPermissions.Open, CashPermissions.View, CustomerPermissions.View, CatalogPermissions.View
    ];

    public ReturnsTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private static async Task<int> ReasonAsync(HttpClient client, string code) =>
        (await client.GetFromJsonAsync<ReturnCatalogsResponse>("api/devoluciones/catalogos"))!.Reasons.Single(reason => reason.Code == code).Id;

    private static async Task<SaveReturnRequest> ReturnAsync(HttpClient client, SaleResponse sale, string reason, params (int Book, int Quantity)[] lines)
    {
        int reasonId = await ReasonAsync(client, reason);

        return new SaveReturnRequest
        {
            SaleId = sale.Id,
            Lines = lines.Select(line => new ReturnLineRequest
            {
                SaleLineId = sale.Lines.Single(item => item.BookId == line.Book).LineId,
                Quantity = line.Quantity,
                ReasonId = reasonId,
                Notes = "El cliente recibió otro título"
            }).ToList()
        };
    }

    private Task<decimal> AvailableAsync(int book) =>
        Factory.QueryAsync(context => context.Existencias
            .Where(item => item.EXI_LibroId == book)
            .SumAsync(item => item.EXI_CantidadDisponible));

    [Fact]
    public async Task VentaPagada_ReembolsaPorCajaYReingresaAlInventario()
    {
        HttpClient admin = await AdminAsync();
        CashSessionResponse session = await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m, stock: 5);

        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 270m)], (book, 3, 10m))), HttpStatusCode.Created);
        Assert.Equal(270m, sale.Total);

        ReturnResponse first = await OkAsync<ReturnResponse>(
            await admin.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "INCORRECTO", (book, 2))), HttpStatusCode.Created);

        // 2 de 3 unidades con el 10 % de descuento ya aplicado.
        Assert.Equal((180m, 0m, 180m), (first.Total, first.CreditApplied, first.Refunded));
        Assert.Matches(@"^DEV-\d{6}$", first.Number);
        Assert.NotNull(first.InventoryMovementNumber);
        Assert.Equal(4m, await AvailableAsync(book));

        PB_LOTE lot = await Factory.QueryAsync(context => context.Lotes.SingleAsync(item => item.LOT_LibroId == book && item.LOT_Numero == first.Number));
        Assert.Equal((2m, 50m), (lot.LOT_CantidadActual, lot.LOT_CostoUnitario));

        CashSessionResponse cash = (await admin.GetFromJsonAsync<CashSessionResponse>($"api/caja/aperturas/{session.Id}"))!;
        Assert.Contains(cash.Movements, movement => movement.TypeCode == CashMovementTypes.Refund && movement.Amount == 180m);

        SaleResponse afterFirst = (await admin.GetFromJsonAsync<SaleResponse>($"api/ventas/{sale.Id}"))!;
        Assert.Equal((SaleStatus.Paid, 2m, 180m), (afterFirst.StatusCode, afterFirst.Lines.Single().ReturnedQuantity, afterFirst.Returned));
        Assert.False(afterFirst.CanAnnul);

        // La última unidad toma el resto exacto y la venta queda devuelta.
        ReturnResponse last = await OkAsync<ReturnResponse>(
            await admin.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "INCORRECTO", (book, 1))), HttpStatusCode.Created);
        Assert.Equal(90m, last.Total);

        SaleResponse returned = (await admin.GetFromJsonAsync<SaleResponse>($"api/ventas/{sale.Id}"))!;
        Assert.Equal((SaleStatus.Returned, false), (returned.StatusCode, returned.CanReturn));

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "INCORRECTO", (book, 1)))).StatusCode);
    }

    [Fact]
    public async Task VentaAlCredito_AbonaPrimeroAlSaldoYReembolsaElResto()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int customer = await CreateCustomerAsync(admin, creditLimit: 2000m, creditDays: 30);
        int book = await CreateBookAsync(admin, price: 150m, stock: 4);

        DirectSaleRequest request = Sale(customer, [await CashAsync(admin, 400m)], (book, 4, 0m));
        request.SaleType = SaleType.Credit;
        SaleResponse sale = await OkAsync<SaleResponse>(await SellAsync(admin, request), HttpStatusCode.Created);
        Assert.Equal((600m, 200m), (sale.Total, sale.Balance));

        ReturnResponse entry = await OkAsync<ReturnResponse>(
            await admin.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "CAMBIO", (book, 2))), HttpStatusCode.Created);

        Assert.Equal((300m, 200m, 100m), (entry.Total, entry.CreditApplied, entry.Refunded));

        SaleResponse after = (await admin.GetFromJsonAsync<SaleResponse>($"api/ventas/{sale.Id}"))!;
        Assert.Equal((0m, SaleStatus.Paid), (after.Balance, after.StatusCode));

        // Anular la devolución: la venta vuelve a deber y el inventario se retira.
        ReturnResponse annulled = await OkAsync<ReturnResponse>(
            await admin.PostAsJsonAsync($"api/devoluciones/{entry.Id}/anular", new AnnulReturnRequest { Reason = "Se registró por error" }));
        Assert.True(annulled.Annulled);

        SaleResponse restored = (await admin.GetFromJsonAsync<SaleResponse>($"api/ventas/{sale.Id}"))!;
        Assert.Equal((200m, SaleStatus.Partial, 0m, true), (restored.Balance, restored.StatusCode, restored.Lines.Single().ReturnedQuantity, restored.CanAnnul));
        Assert.Equal(0m, await AvailableAsync(book));
    }

    [Fact]
    public async Task MotivoDeBaja_NoReingresaYExigeObservacion()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 80m, stock: 2);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 160m)], (book, 2, 0m))), HttpStatusCode.Created);

        SaveReturnRequest withoutNotes = await ReturnAsync(admin, sale, "DEFECTUOSO", (book, 1));
        withoutNotes.Lines[0].Notes = null;
        HttpResponseMessage invalid = await admin.PostAsJsonAsync("api/devoluciones", withoutNotes);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("requiere una observación", await ReadProblemDetailAsync(invalid));

        ReturnResponse entry = await OkAsync<ReturnResponse>(
            await admin.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "DEFECTUOSO", (book, 1))), HttpStatusCode.Created);

        Assert.Null(entry.InventoryMovementNumber);
        Assert.False(Assert.Single(entry.Lines).Restocks);
        Assert.Equal(0m, await AvailableAsync(book));
    }

    [Fact]
    public async Task FueraDelPlazo_NoSeAceptaLaDevolucion()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 60m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 60m)], (book, 1, 0m))), HttpStatusCode.Created);

        await Factory.QueryAsync(async context =>
        {
            var entity = await context.Ventas.SingleAsync(item => item.VEN_Venta == sale.Id);
            entity.VEN_Fecha = DateTime.UtcNow.AddDays(-45);
            return await context.SaveChangesAsync();
        });

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "INCORRECTO", (book, 1)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("30 días", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task ReembolsoEnEfectivo_SinEfectivoSuficienteEnCaja_Devuelve409()
    {
        HttpClient admin = await AdminAsync();
        (HttpClient cashier, _) = await EmployeeClientAsync(Cashier);
        await EnsureCashAsync(cashier); // fondo inicial Q100
        int book = await CreateBookAsync(admin, price: 300m, stock: 1);

        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(cashier, Sale(null, [await CardAsync(cashier, 300m)], (book, 1, 0m))), HttpStatusCode.Created);

        HttpResponseMessage cash = await cashier.PostAsJsonAsync("api/devoluciones", await ReturnAsync(cashier, sale, "INCORRECTO", (book, 1)));
        Assert.Equal(HttpStatusCode.Conflict, cash.StatusCode);
        Assert.Contains("No hay suficiente efectivo", await ReadProblemDetailAsync(cash));

        // Con tarjeta (reversión del cargo) sí procede.
        SaveReturnRequest byCard = await ReturnAsync(cashier, sale, "INCORRECTO", (book, 1));
        byCard.RefundMethodId = await MethodAsync(cashier, PaymentMethodCode.Card);
        byCard.RefundReference = "Reversión 998877";
        ReturnResponse entry = await OkAsync<ReturnResponse>(await cashier.PostAsJsonAsync("api/devoluciones", byCard), HttpStatusCode.Created);
        Assert.Equal(PaymentMethodCode.Card, Assert.Single(entry.Refunds).PaymentMethodCode);

        // El cajero no puede anular la devolución ni la venta tiene anulación directa.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.PostAsJsonAsync($"api/devoluciones/{entry.Id}/anular", new AnnulReturnRequest { Reason = "Prueba de permiso" })).StatusCode);
        HttpResponseMessage annulSale = await admin.PostAsJsonAsync($"api/ventas/{sale.Id}/anular", new AnnulSaleRequest { Reason = "Prueba de bloqueo" });
        Assert.Equal(HttpStatusCode.Conflict, annulSale.StatusCode);
        Assert.Contains("devoluciones", await ReadProblemDetailAsync(annulSale));
    }

    [Fact]
    public async Task Permisos_SinVentasDevolver_Devuelve403()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 50m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 50m)], (book, 1, 0m))), HttpStatusCode.Created);

        (HttpClient seller, _) = await EmployeeClientAsync(SalesPermissions.View, SalesPermissions.Create);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await seller.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "INCORRECTO", (book, 1)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await seller.GetAsync("api/devoluciones")).StatusCode);
    }

    [Fact]
    public async Task Auditoria_RegistraLaDevolucionYElCambioEnLaVenta()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 70m, stock: 1);
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 70m)], (book, 1, 0m))), HttpStatusCode.Created);

        ReturnResponse entry = await OkAsync<ReturnResponse>(
            await admin.PostAsJsonAsync("api/devoluciones", await ReturnAsync(admin, sale, "INCORRECTO", (book, 1))), HttpStatusCode.Created);

        List<string> actions = await Factory.QueryAsync(context => context.Auditorias
            .Where(item => (item.AUD_Entidad == "DevolucionVenta" && item.AUD_EntidadId == entry.Id.ToString()) ||
                           (item.AUD_Entidad == "Venta" && item.AUD_EntidadId == sale.Id.ToString() && item.AUD_Accion == "DEVOLVER"))
            .Select(item => item.AUD_Entidad + ":" + item.AUD_Accion)
            .ToListAsync());

        Assert.Equal(["DevolucionVenta:DEVOLVER", "Venta:DEVOLVER"], actions.Order().ToArray());

        PagedResponse<ReturnListItemResponse> list = (await admin.GetFromJsonAsync<PagedResponse<ReturnListItemResponse>>($"api/devoluciones?search={sale.Number}"))!;
        Assert.Equal(entry.Id, Assert.Single(list.Items).Id);
    }
}
