using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.IntegrationTests.Infrastructure;

namespace TikalLibros.IntegrationTests.Api;

/// <summary>
/// Flujo completo entre módulos: compra → traslado → venta → devolución del
/// cliente → devolución al proveedor. Comprueba que existencias, lotes,
/// kardex, caja, reportes y auditoría cuentan la misma historia.
/// </summary>
public sealed class EndToEndFlowTests : SalesTestBase
{
    public EndToEndFlowTests(TikalLibrosApiFactory factory)
        : base(factory)
    {
    }

    private Task<decimal> StockInAsync(int warehouse, int book) =>
        Factory.QueryAsync(context => context.Existencias
            .Where(item => item.EXI_AlmacenId == warehouse && item.EXI_LibroId == book)
            .SumAsync(item => item.EXI_CantidadDisponible));

    [Fact]
    public async Task CompraTrasladoVentaYDevoluciones_MantienenConsistentesInventarioCajaReportesYAuditoria()
    {
        HttpClient admin = await AdminAsync();
        CashSessionResponse session = await EnsureCashAsync(admin);
        SalesReportResponse salesBefore = (await admin.GetFromJsonAsync<SalesReportResponse>("api/reportes/ventas"))!;

        int main = await Factory.QueryAsync(context => context.Almacenes
            .Where(item => item.ALM_Codigo == OrganizationSeeder.MainWarehouseCode).Select(item => item.ALM_Almacen).SingleAsync());
        WarehouseResponse mainWarehouse = (await admin.GetFromJsonAsync<WarehouseResponse>($"api/almacenes/{main}"))!;
        WarehouseResponse second = await OkAsync<WarehouseResponse>(await admin.PostAsJsonAsync("api/almacenes",
            new SaveWarehouseRequest { BranchId = mainWarehouse.BranchId, Name = Unique("Bodega de prueba ") }), HttpStatusCode.Created);

        // 1. Compra: 10 unidades a Q50 (precio de venta Q112 con IVA).
        int book = await CreateBookAsync(admin, price: 112m);
        await AddStockAsync(admin, book, 10, cost: 50m);

        // 2. Traslado de 3 unidades a la segunda bodega.
        await OkAsync<MovementResponse>(await admin.PostAsJsonAsync("api/inventario/traslados", new SaveTransferRequest
        {
            SourceWarehouseId = main,
            TargetWarehouseId = second.Id,
            Lines = [new TransferLineRequest { BookId = book, Quantity = 3 }]
        }), HttpStatusCode.Created);

        // 3. Venta de 4 unidades al contado.
        SaleResponse sale = await OkAsync<SaleResponse>(
            await SellAsync(admin, Sale(null, [await CashAsync(admin, 448m)], (book, 4, 0m))), HttpStatusCode.Created);

        // 4. El cliente devuelve 1 (vuelve al inventario y se reembolsa por caja).
        int reason = (await admin.GetFromJsonAsync<ReturnCatalogsResponse>("api/devoluciones/catalogos"))!.Reasons.Single(item => item.Code == "INCORRECTO").Id;
        ReturnResponse customerReturn = await OkAsync<ReturnResponse>(await admin.PostAsJsonAsync("api/devoluciones", new SaveReturnRequest
        {
            SaleId = sale.Id,
            Lines = [new ReturnLineRequest { SaleLineId = sale.Lines.Single().LineId, Quantity = 1, ReasonId = reason, Notes = "Edición equivocada" }]
        }), HttpStatusCode.Created);

        // 5. Se devuelve 1 unidad al proveedor del lote recibido.
        var receptionLine = await Factory.QueryAsync(context => context.RecepcionesCompraDetalles
            .Where(item => item.RCD_LibroId == book)
            .Select(item => new { item.RCD_RecepcionCompraId, item.RCD_RecepcionCompraDetalle })
            .SingleAsync());
        SupplierReturnResponse supplierReturn = await OkAsync<SupplierReturnResponse>(await admin.PostAsJsonAsync("api/devoluciones-proveedor", new SaveSupplierReturnRequest
        {
            ReceptionId = receptionLine.RCD_RecepcionCompraId,
            Reason = "Ejemplar con defecto de impresión",
            Lines = [new SupplierReturnLineRequest { ReceptionLineId = receptionLine.RCD_RecepcionCompraDetalle, Quantity = 1 }]
        }), HttpStatusCode.Created);

        // Existencias: principal 10 − 3 − 4 + 1 − 1 = 3; segunda bodega 3. Los lotes cuadran.
        Assert.Equal(3m, await StockInAsync(main, book));
        Assert.Equal(3m, await StockInAsync(second.Id, book));
        Assert.Equal(6m, await Factory.QueryAsync(context => context.Lotes.Where(lot => lot.LOT_LibroId == book).SumAsync(lot => lot.LOT_CantidadActual)));

        // Kardex de la bodega principal: entradas 10 + 1, salidas 3 + 4 + 1.
        KardexResponse kardex = (await admin.GetFromJsonAsync<KardexResponse>($"api/inventario/kardex?bookId={book}&warehouseId={main}"))!;
        Assert.Equal((11m, 8m, 3m), (kardex.TotalIn, kardex.TotalOut, kardex.ClosingBalance));

        // Caja: el cobro y el reembolso están en la apertura.
        CashSessionResponse cash = (await admin.GetFromJsonAsync<CashSessionResponse>($"api/caja/aperturas/{session.Id}"))!;
        Assert.Contains(cash.Movements, movement => movement.TypeCode == "VENTA" && movement.Amount == 448m);
        Assert.Contains(cash.Movements, movement => movement.TypeCode == CashMovementTypes.Refund && movement.Amount == 112m);

        // Reportes: venta y devolución del período; libros más vendidos neto.
        SalesReportResponse salesAfter = (await admin.GetFromJsonAsync<SalesReportResponse>("api/reportes/ventas"))!;
        Assert.Equal((448m, 112m), (salesAfter.Total - salesBefore.Total, salesAfter.Returned - salesBefore.Returned));
        TopBooksResponse top = (await admin.GetFromJsonAsync<TopBooksResponse>("api/reportes/libros-mas-vendidos?top=100"))!;
        Assert.Equal(3m, top.Rows.Single(row => row.BookId == book).Units);

        PurchasesReportResponse purchases = (await admin.GetFromJsonAsync<PurchasesReportResponse>("api/reportes/compras"))!;
        Assert.Contains(purchases.TopBooks, row => row.BookId == book && row.Total == 500m);

        // Auditoría de cada documento.
        string[] audited = (await Factory.QueryAsync(context => context.Auditorias
                .Where(item => (item.AUD_Entidad == "Venta" && item.AUD_EntidadId == sale.Id.ToString()) ||
                               (item.AUD_Entidad == "DevolucionVenta" && item.AUD_EntidadId == customerReturn.Id.ToString()) ||
                               (item.AUD_Entidad == "DevolucionProveedor" && item.AUD_EntidadId == supplierReturn.Id.ToString()) ||
                               item.AUD_Entidad == "TrasladoInventario")
                .Select(item => item.AUD_Entidad)
                .Distinct()
                .ToListAsync()))
            .Order()
            .ToArray();
        Assert.Equal(["DevolucionProveedor", "DevolucionVenta", "TrasladoInventario", "Venta"], audited);
    }
}
