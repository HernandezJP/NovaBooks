using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Devoluciones al proveedor: salen del lote de la recepción, solo unidades
/// libres, valor al costo, kardex, anulación y relación con la recepción.
/// </summary>
public sealed class SupplierReturnsTests : SalesTestBase
{
    public SupplierReturnsTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    /// <summary>Recepción (compra directa) del libro y su línea.</summary>
    private Task<(int Reception, int Line)> ReceptionOfAsync(int book) =>
        Factory.QueryAsync(async context => await context.RecepcionesCompraDetalles
            .Where(line => line.RCD_LibroId == book)
            .OrderByDescending(line => line.RCD_RecepcionCompraDetalle)
            .Select(line => new ValueTuple<int, int>(line.RCD_RecepcionCompraId, line.RCD_RecepcionCompraDetalle))
            .FirstAsync());

    private Task<decimal> AvailableAsync(int book) =>
        Factory.QueryAsync(context => context.Existencias.Where(item => item.EXI_LibroId == book).SumAsync(item => item.EXI_CantidadDisponible));

    private static SaveSupplierReturnRequest Return(int reception, int line, int quantity) => new()
    {
        ReceptionId = reception,
        Reason = "Ejemplares con defecto de impresión",
        Lines = [new SupplierReturnLineRequest { ReceptionLineId = line, Quantity = quantity }]
    };

    [Fact]
    public async Task Devolucion_SaleDelLoteDeLaRecepcionAlCostoYSeAnula()
    {
        HttpClient admin = await AdminAsync();
        int book = await CreateBookAsync(admin);
        await AddStockAsync(admin, book, 10, cost: 42.5m);
        (int reception, int line) = await ReceptionOfAsync(book);

        SupplierReturnResponse entry = await OkAsync<SupplierReturnResponse>(
            await admin.PostAsJsonAsync("api/devoluciones-proveedor", Return(reception, line, 3)), HttpStatusCode.Created);

        Assert.Equal((3m, 42.5m, 127.5m), (Assert.Single(entry.Lines).Quantity, entry.Lines[0].UnitCost, entry.Total));
        Assert.Equal("Ejemplares con defecto de impresión", entry.Reason);
        Assert.Equal(7m, await AvailableAsync(book));

        ReturnableReceptionResponse returnable = (await admin.GetFromJsonAsync<ReturnableReceptionResponse>($"api/devoluciones-proveedor/recepciones/{reception}"))!;
        Assert.Equal((10m, 7m), (returnable.Lines.Single().Accepted, returnable.Lines.Single().Returnable));

        KardexResponse kardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}"))!;
        Assert.Equal((10m, 3m, 7m), (kardex.TotalIn, kardex.TotalOut, kardex.ClosingBalance));

        Assert.True(await Factory.QueryAsync(context => context.Auditorias.AnyAsync(item =>
            item.AUD_Entidad == "DevolucionProveedor" && item.AUD_EntidadId == entry.Id.ToString() && item.AUD_Accion == "DEVOLVER")));

        // Con mercadería devuelta la recepción ya no puede anularse.
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync($"api/recepciones/{reception}/anular", new AnnulReceptionRequest { Reason = "Prueba de bloqueo" })).StatusCode);

        SupplierReturnResponse annulled = await OkAsync<SupplierReturnResponse>(
            await admin.PostAsJsonAsync($"api/devoluciones-proveedor/{entry.Id}/anular", new AnnulSupplierReturnRequest { Reason = "El proveedor no la aceptó" }));
        Assert.True(annulled.Annulled);
        Assert.Equal(10m, await AvailableAsync(book));
        Assert.Equal(10m, await Factory.QueryAsync(context => context.Lotes.Where(lot => lot.LOT_LibroId == book).SumAsync(lot => lot.LOT_CantidadActual)));

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync($"api/devoluciones-proveedor/{entry.Id}/anular", new AnnulSupplierReturnRequest { Reason = "Otra vez" })).StatusCode);

        PagedResponse<SupplierReturnListItemResponse> list = (await admin.GetFromJsonAsync<PagedResponse<SupplierReturnListItemResponse>>(
            $"api/devoluciones-proveedor?search={entry.ReceptionNumber}"))!;
        Assert.True(Assert.Single(list.Items).Annulled);
    }

    [Fact]
    public async Task Devolucion_NoIncluyeUnidadesVendidasNiReservadas()
    {
        HttpClient admin = await AdminAsync();
        await EnsureCashAsync(admin);
        int book = await CreateBookAsync(admin, price: 100m);
        await AddStockAsync(admin, book, 10);
        (int reception, int line) = await ReceptionOfAsync(book);

        await OkAsync<SaleResponse>(await SellAsync(admin, Sale(null, [await CashAsync(admin, 600m)], (book, 6, 0m))), HttpStatusCode.Created);

        await Factory.QueryAsync(async context =>
        {
            PB_EXISTENCIA stock = await context.Existencias.SingleAsync(item => item.EXI_LibroId == book);
            stock.EXI_CantidadReservada = 1;
            return await context.SaveChangesAsync();
        });

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/devoluciones-proveedor", Return(reception, line, 4));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("quedan 3 libres", await ReadProblemDetailAsync(response));
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("api/devoluciones-proveedor", Return(reception, line, 3))).StatusCode);
    }

    [Fact]
    public async Task Devolucion_LineaAjenaOMotivoCorto_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int book = await CreateBookAsync(admin, stock: 2);
        int other = await CreateBookAsync(admin, stock: 2);
        (int reception, _) = await ReceptionOfAsync(book);
        (_, int otherLine) = await ReceptionOfAsync(other);

        HttpResponseMessage foreign = await admin.PostAsJsonAsync("api/devoluciones-proveedor", Return(reception, otherLine, 1));
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Contains("no pertenece", await ReadProblemDetailAsync(foreign));

        SaveSupplierReturnRequest shortReason = Return(reception, otherLine, 1);
        shortReason.Reason = "Mal";
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/devoluciones-proveedor", shortReason)).StatusCode);
    }

    [Fact]
    public async Task Permisos_DevolverYAnularRequierenSusPermisos()
    {
        HttpClient admin = await AdminAsync();
        int book = await CreateBookAsync(admin, stock: 4);
        (int reception, int line) = await ReceptionOfAsync(book);

        (HttpClient viewer, _) = await EmployeeClientAsync(PurchasingPermissions.View);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("api/devoluciones-proveedor")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("api/devoluciones-proveedor", Return(reception, line, 1))).StatusCode);

        (HttpClient buyer, _) = await EmployeeClientAsync(PurchasingPermissions.View, PurchasingPermissions.Return);
        SupplierReturnResponse entry = await OkAsync<SupplierReturnResponse>(
            await buyer.PostAsJsonAsync("api/devoluciones-proveedor", Return(reception, line, 1)), HttpStatusCode.Created);
        Assert.NotNull(entry.Employee);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await buyer.PostAsJsonAsync($"api/devoluciones-proveedor/{entry.Id}/anular", new AnnulSupplierReturnRequest { Reason = "Sin permiso" })).StatusCode);
    }
}
