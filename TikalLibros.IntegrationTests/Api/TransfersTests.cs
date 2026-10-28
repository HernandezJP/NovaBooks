using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Traslados entre almacenes: PEPS en el origen, lotes con el mismo costo en
/// el destino, solo unidades libres, kardex y anulación.
/// </summary>
public sealed class TransfersTests : SalesTestBase
{
    public TransfersTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private Task<int> MainWarehouseAsync() =>
        Factory.QueryAsync(context => context.Almacenes
            .Where(item => item.ALM_Codigo == OrganizationSeeder.MainWarehouseCode)
            .Select(item => item.ALM_Almacen)
            .SingleAsync());

    private static async Task<int> CreateWarehouseAsync(HttpClient admin, int warehouseInSameBranch)
    {
        WarehouseResponse reference = (await admin.GetFromJsonAsync<WarehouseResponse>($"api/almacenes/{warehouseInSameBranch}"))!;

        return (await OkAsync<WarehouseResponse>(await admin.PostAsJsonAsync("api/almacenes", new SaveWarehouseRequest
        {
            BranchId = reference.BranchId,
            Name = Unique("Bodega de tránsito ")
        }), HttpStatusCode.Created)).Id;
    }

    private Task<(decimal Available, decimal Reserved)> StockInAsync(int warehouse, int book) =>
        Factory.QueryAsync(async context => await context.Existencias
            .Where(item => item.EXI_AlmacenId == warehouse && item.EXI_LibroId == book)
            .Select(item => new ValueTuple<decimal, decimal>(item.EXI_CantidadDisponible, item.EXI_CantidadReservada))
            .SingleOrDefaultAsync());

    private Task<List<PB_LOTE>> LotsInAsync(int warehouse, int book) =>
        Factory.QueryAsync(context => context.Lotes
            .Where(item => item.LOT_AlmacenId == warehouse && item.LOT_LibroId == book)
            .OrderBy(item => item.LOT_CostoUnitario)
            .ToListAsync());

    private static SaveTransferRequest Transfer(int source, int target, params (int Book, int Quantity)[] lines) => new()
    {
        SourceWarehouseId = source,
        TargetWarehouseId = target,
        Notes = "Reposición de la sucursal",
        Lines = lines.Select(line => new TransferLineRequest { BookId = line.Book, Quantity = line.Quantity }).ToList()
    };

    [Fact]
    public async Task Traslado_SalePorPepsYLlegaConLosMismosLotesYCostos()
    {
        HttpClient admin = await AdminAsync();
        int main = await MainWarehouseAsync();
        int target = await CreateWarehouseAsync(admin, main);
        int book = await CreateBookAsync(admin);
        await AddStockAsync(admin, book, 5, cost: 40m);
        await AddStockAsync(admin, book, 5, cost: 60m);

        MovementResponse transfer = await OkAsync<MovementResponse>(
            await admin.PostAsJsonAsync("api/inventario/traslados", Transfer(main, target, (book, 7))), HttpStatusCode.Created);

        Assert.Equal("TRANSFERENCIA", transfer.TypeCode);
        Assert.Equal([(5m, 40m), (2m, 60m)], transfer.Lines.Select(line => (line.Quantity, line.UnitCost!.Value)).ToArray());

        Assert.Equal((3m, 0m), await StockInAsync(main, book));
        Assert.Equal((7m, 0m), await StockInAsync(target, book));

        List<PB_LOTE> lots = await LotsInAsync(target, book);
        Assert.Equal([(40m, 5m), (60m, 2m)], lots.Select(lot => (lot.LOT_CostoUnitario, lot.LOT_CantidadActual)).ToArray());
        Assert.Equal(transfer.Lines.Select(line => line.LotNumber).ToArray(), lots.Select(lot => (string?)lot.LOT_Numero).ToArray());

        // El kardex del destino registra la entrada y el del origen la salida.
        KardexResponse targetKardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}&warehouseId={target}"))!;
        KardexResponse sourceKardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}&warehouseId={main}"))!;
        Assert.Equal((7m, 7m), (targetKardex.TotalIn, targetKardex.ClosingBalance));
        Assert.Equal((10m, 7m, 3m), (sourceKardex.TotalIn, sourceKardex.TotalOut, sourceKardex.ClosingBalance));

        Assert.True(await Factory.QueryAsync(context => context.Auditorias.AnyAsync(item =>
            item.AUD_Entidad == "TrasladoInventario" && item.AUD_EntidadId == transfer.Id.ToString())));
    }

    [Fact]
    public async Task Traslado_NoMueveUnidadesReservadas()
    {
        HttpClient admin = await AdminAsync();
        int main = await MainWarehouseAsync();
        int target = await CreateWarehouseAsync(admin, main);
        int book = await CreateBookAsync(admin, stock: 6);

        await Factory.QueryAsync(async context =>
        {
            PB_EXISTENCIA stock = await context.Existencias.SingleAsync(item => item.EXI_AlmacenId == main && item.EXI_LibroId == book);
            stock.EXI_CantidadReservada = 4;
            return await context.SaveChangesAsync();
        });

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/inventario/traslados", Transfer(main, target, (book, 3)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("2 libres", await ReadProblemDetailAsync(response));
        Assert.Equal((6m, 4m), await StockInAsync(main, book));
    }

    [Fact]
    public async Task Traslado_MismoAlmacenODestinoInactivo_Devuelve400()
    {
        HttpClient admin = await AdminAsync();
        int main = await MainWarehouseAsync();
        int book = await CreateBookAsync(admin, stock: 2);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("api/inventario/traslados", Transfer(main, main, (book, 1)))).StatusCode);

        int inactive = await CreateWarehouseAsync(admin, main);
        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"api/almacenes/{inactive}/estado",
            new Application.DTOs.Common.ChangeActiveStatusRequest { IsActive = false })).StatusCode);

        HttpResponseMessage response = await admin.PostAsJsonAsync("api/inventario/traslados", Transfer(main, inactive, (book, 1)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("destino", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Anular_DevuelveLasUnidadesSoloSiSiguenLibresEnElDestino()
    {
        HttpClient admin = await AdminAsync();
        int main = await MainWarehouseAsync();
        int second = await CreateWarehouseAsync(admin, main);
        int third = await CreateWarehouseAsync(admin, main);
        int book = await CreateBookAsync(admin, stock: 10);

        MovementResponse first = await OkAsync<MovementResponse>(
            await admin.PostAsJsonAsync("api/inventario/traslados", Transfer(main, second, (book, 4))), HttpStatusCode.Created);
        MovementResponse onward = await OkAsync<MovementResponse>(
            await admin.PostAsJsonAsync("api/inventario/traslados", Transfer(second, third, (book, 3))), HttpStatusCode.Created);

        // Las unidades ya salieron del destino: el primer traslado no puede anularse.
        HttpResponseMessage blocked = await admin.PostAsJsonAsync($"api/inventario/traslados/{first.Id}/anular", new AnnulTransferRequest { Reason = "Error de captura" });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        MovementResponse annulled = await OkAsync<MovementResponse>(
            await admin.PostAsJsonAsync($"api/inventario/traslados/{onward.Id}/anular", new AnnulTransferRequest { Reason = "Se envió a la bodega equivocada" }));
        Assert.True(annulled.Annulled);
        Assert.Equal("Se envió a la bodega equivocada", annulled.AnnulmentReason);

        Assert.Equal(HttpStatusCode.OK,
            (await admin.PostAsJsonAsync($"api/inventario/traslados/{first.Id}/anular", new AnnulTransferRequest { Reason = "Error de captura" })).StatusCode);

        Assert.Equal((10m, 0m), await StockInAsync(main, book));
        Assert.Equal((0m, 0m), await StockInAsync(second, book));
        Assert.Equal((0m, 0m), await StockInAsync(third, book));
        Assert.Equal(10m, (await LotsInAsync(main, book)).Sum(lot => lot.LOT_CantidadActual));

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync($"api/inventario/traslados/{first.Id}/anular", new AnnulTransferRequest { Reason = "Otra vez" })).StatusCode);

        // El kardex ya no cuenta los traslados anulados.
        KardexResponse kardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}&warehouseId={main}"))!;
        Assert.Equal((0m, 10m), (kardex.TotalOut, kardex.ClosingBalance));

        PagedResponse<TransferListItemResponse> list = (await admin.GetFromJsonAsync<PagedResponse<TransferListItemResponse>>(
            $"api/inventario/traslados?warehouseId={third}"))!;
        Assert.True(Assert.Single(list.Items).Annulled);
        Assert.Empty((await admin.GetFromJsonAsync<PagedResponse<TransferListItemResponse>>(
            $"api/inventario/traslados?warehouseId={third}&includeAnnulled=false"))!.Items);
    }

    [Fact]
    public async Task Traslado_LoteYaExistenteConOtroCosto_CreaLoteConNumeroDelTraslado()
    {
        HttpClient admin = await AdminAsync();
        int main = await MainWarehouseAsync();
        int target = await CreateWarehouseAsync(admin, main);
        int book = await CreateBookAsync(admin);
        await AddStockAsync(admin, book, 3, cost: 45m);

        string lotNumber = (await LotsInAsync(main, book)).Single().LOT_Numero;

        await Factory.QueryAsync(async context =>
        {
            context.Lotes.Add(new PB_LOTE
            {
                LOT_AlmacenId = target, LOT_LibroId = book, LOT_Numero = lotNumber, LOT_FechaIngreso = DateOnly.FromDateTime(DateTime.Today),
                LOT_CantidadInicial = 1, LOT_CantidadActual = 0, LOT_CostoUnitario = 99m, LOT_Activo = false
            });
            return await context.SaveChangesAsync();
        });

        MovementResponse transfer = await OkAsync<MovementResponse>(
            await admin.PostAsJsonAsync("api/inventario/traslados", Transfer(main, target, (book, 2))), HttpStatusCode.Created);

        PB_LOTE created = (await LotsInAsync(target, book)).Single(lot => lot.LOT_CantidadActual > 0);
        Assert.Equal(($"{lotNumber}/{transfer.Number}", 45m, 2m), (created.LOT_Numero, created.LOT_CostoUnitario, created.LOT_CantidadActual));

        Assert.Equal(HttpStatusCode.OK,
            (await admin.PostAsJsonAsync($"api/inventario/traslados/{transfer.Id}/anular", new AnnulTransferRequest { Reason = "Prueba de anulación" })).StatusCode);
        Assert.Equal(0m, (await LotsInAsync(target, book)).Sum(lot => lot.LOT_CantidadActual));
    }

    [Fact]
    public async Task Permisos_TrasladarRequiereInventarioTransferir()
    {
        HttpClient admin = await AdminAsync();
        int main = await MainWarehouseAsync();
        int target = await CreateWarehouseAsync(admin, main);
        int book = await CreateBookAsync(admin, stock: 3);

        (HttpClient viewer, _) = await EmployeeClientAsync(InventoryPermissions.View);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("api/inventario/traslados")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("api/inventario/traslados", Transfer(main, target, (book, 1)))).StatusCode);

        (HttpClient warehouseKeeper, _) = await EmployeeClientAsync(InventoryPermissions.Transfer);
        MovementResponse transfer = await OkAsync<MovementResponse>(
            await warehouseKeeper.PostAsJsonAsync("api/inventario/traslados", Transfer(main, target, (book, 1))), HttpStatusCode.Created);

        // Sin Inventario.VerCostos no se envían los costos.
        Assert.All(transfer.Lines, line => Assert.Null(line.UnitCost));
        Assert.NotNull(transfer.Employee);
    }
}
