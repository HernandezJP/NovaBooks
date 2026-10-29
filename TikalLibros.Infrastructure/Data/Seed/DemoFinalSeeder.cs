using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Sales;
using TikalLibros.Infrastructure.Data.Identity;
using TikalLibros.Infrastructure.Services.Sales;

namespace TikalLibros.Infrastructure.Data.Seed;

/// <summary>
/// Datos de demostración de la entrega final (solo en desarrollo, después de
/// la demo de ventas): una segunda sucursal con bodega y caja, ventas de los
/// meses anteriores para los gráficos, traslados (uno anulado), devoluciones
/// de clientes (contado con reingreso, crédito abonado al saldo y producto
/// dañado), una devolución al proveedor y situaciones que activan alertas
/// (crédito vencido y caja abierta desde ayer). Usa los servicios de la
/// aplicación para que inventario, caja, saldos y auditoría queden
/// consistentes. Las operaciones se cargan una sola vez: si ya hay
/// devoluciones de clientes, no se repiten.
/// </summary>
public sealed class DemoFinalSeeder
{
    public const string BranchName = "Sucursal Xela";
    public const string WarehouseName = "Bodega Xela";
    public const string CashRegisterName = "Caja Xela";

    private static readonly SalesPermissionSet Supervisor = new(true, true, true);

    private readonly AppDbContext _context;
    private readonly IBranchService _branches;
    private readonly IWarehouseService _warehouses;
    private readonly ICashRegisterService _cashRegisters;
    private readonly ICashService _cash;
    private readonly ISaleService _sales;
    private readonly ITransferService _transfers;
    private readonly IReturnService _returns;
    private readonly ISupplierReturnService _supplierReturns;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoFinalSeeder> _logger;

    public DemoFinalSeeder(
        AppDbContext context,
        IBranchService branches,
        IWarehouseService warehouses,
        ICashRegisterService cashRegisters,
        ICashService cash,
        ISaleService sales,
        ITransferService transfers,
        IReturnService returns,
        ISupplierReturnService supplierReturns,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<DemoFinalSeeder> logger)
    {
        _context = context;
        _branches = branches;
        _warehouses = warehouses;
        _cashRegisters = cashRegisters;
        _cash = cash;
        _sales = sales;
        _transfers = transfers;
        _returns = returns;
        _supplierReturns = supplierReturns;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        string? adminName = _configuration["IdentitySeed:Administrator:UserName"];
        ApplicationUser? admin = string.IsNullOrWhiteSpace(adminName) ? null : await _userManager.FindByNameAsync(adminName);

        if (admin?.USU_EmpleadoId is null ||
            !await _context.Ventas.AnyAsync(cancellationToken) ||
            await _context.DevolucionesVenta.AnyAsync(cancellationToken))
        {
            return;
        }

        (int warehouse, int register)? xela = await EnsureXelaAsync(cancellationToken);

        if (xela is null)
        {
            return;
        }

        int? session = await OpenSessionAsync(admin.Id, admin.USU_EmpleadoId.Value, xela.Value.register, cancellationToken);

        if (session is null)
        {
            return;
        }

        Dictionary<string, PaymentMethodResponse> methods = (await _sales.GetCatalogsAsync(cancellationToken))
            .PaymentMethods.ToDictionary(item => item.Code);

        await SeedHistoryAsync(admin.Id, methods, cancellationToken);
        await SeedTransfersAsync(admin.Id, xela.Value.warehouse, cancellationToken);
        await SeedCustomerReturnsAsync(admin.Id, methods, cancellationToken);
        await SeedSupplierReturnAsync(admin.Id, cancellationToken);
        await SeedAlertsAsync(session.Value, cancellationToken);

        _logger.LogInformation("Datos de demostración de la entrega final cargados.");
    }

    // ───────────────────────────── Sucursal Xela ─────────────────────────────

    private async Task<(int Warehouse, int Register)?> EnsureXelaAsync(CancellationToken cancellationToken)
    {
        int? branch = await _context.Sucursales
            .Where(item => item.SUC_Nombre == BranchName)
            .Select(item => (int?)item.SUC_Sucursal)
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
        {
            int? municipality = await _context.Municipios
                .Where(item => item.MUN_Nombre == "Quetzaltenango" && item.Departamento.DEP_Nombre == "Quetzaltenango")
                .Select(item => (int?)item.MUN_Municipio)
                .FirstOrDefaultAsync(cancellationToken);

            branch = Require(await _branches.CreateAsync(new SaveBranchRequest
            {
                Name = BranchName,
                MunicipalityId = municipality,
                Zone = "1",
                AddressLine = "14 avenida 5-22, frente al parque central",
                AddressReference = "A media cuadra de la catedral",
                Phone = "7761 2345",
                Email = "xela@tikallibros.test"
            }, cancellationToken), "sucursal Xela")?.Id;
        }

        if (branch is null)
        {
            return null;
        }

        int? warehouse = await _context.Almacenes
            .Where(item => item.ALM_SucursalId == branch && item.ALM_Nombre == WarehouseName)
            .Select(item => (int?)item.ALM_Almacen)
            .FirstOrDefaultAsync(cancellationToken)
            ?? Require(await _warehouses.CreateAsync(new SaveWarehouseRequest
            {
                BranchId = branch,
                Name = WarehouseName,
                Description = "Bodega de la tienda de Quetzaltenango."
            }, cancellationToken), "bodega Xela")?.Id;

        int? register = await _context.Cajas
            .Where(item => item.CAJ_SucursalId == branch && item.CAJ_Nombre == CashRegisterName)
            .Select(item => (int?)item.CAJ_Caja)
            .FirstOrDefaultAsync(cancellationToken)
            ?? Require(await _cashRegisters.CreateAsync(new SaveCashRegisterRequest
            {
                BranchId = branch,
                Name = CashRegisterName,
                Description = "Caja de mostrador de la sucursal Xela."
            }, cancellationToken), "caja Xela")?.Id;

        return warehouse is null || register is null ? null : (warehouse.Value, register.Value);
    }

    /// <summary>Caja abierta del administrador (la de Xela si no tiene otra abierta).</summary>
    private async Task<int?> OpenSessionAsync(int adminId, int adminEmployee, int register, CancellationToken cancellationToken)
    {
        int? open = await _context.AperturasCaja
            .Where(item => item.ACA_Abierta && item.ACA_EmpleadoId == adminEmployee)
            .Select(item => (int?)item.ACA_AperturaCaja)
            .FirstOrDefaultAsync(cancellationToken);

        return open ?? Require(await _cash.OpenAsync(
            new OpenCashSessionRequest { CashRegisterId = register, InitialAmount = 1000m, Notes = "Fondo de cambio para devoluciones y ventas." },
            adminId, cancellationToken), "apertura de caja en Xela")?.Id;
    }

    // ───────────────────────────── Ventas de meses anteriores ─────────────────────────────

    /// <summary>
    /// Dos ventas por mes en los cinco meses anteriores, para el gráfico
    /// mensual y la comparación con el mes anterior. Se registran con los
    /// servicios y luego se les asigna la fecha del mes que les toca.
    /// </summary>
    private async Task SeedHistoryAsync(int adminId, Dictionary<string, PaymentMethodResponse> methods, CancellationToken cancellationToken)
    {
        List<(int Id, decimal Price, decimal Tax)> books = await BooksWithStockAsync(8, cancellationToken);

        if (books.Count < 4)
        {
            return;
        }

        DateOnly today = BusinessTime.Today;
        int index = 0;

        for (int monthsBack = 5; monthsBack >= 1; monthsBack--)
        {
            foreach (int day in (int[])[8, 19])
            {
                (int Id, decimal Price, decimal Tax) book = books[index % books.Count];
                int quantity = 1 + index % 3;
                decimal total = SaleLineAmounts.Calculate(quantity, book.Price, 0m, book.Tax).Total;
                bool byCard = index % 2 == 1;
                index++;

                SaleResponse? sale = Require(await _sales.SellAsync(new DirectSaleRequest
                {
                    Lines = [new OrderLineRequest { BookId = book.Id, Quantity = quantity }],
                    Payments = [byCard
                        ? new PaymentRequest { PaymentMethodId = methods[PaymentMethodCode.Card].Id, Amount = total, Authorization = "AUT1234", LastFourDigits = "1234" }
                        : new PaymentRequest { PaymentMethodId = methods[PaymentMethodCode.Cash].Id, Amount = total, Received = total }]
                }, adminId, Supervisor, cancellationToken), "venta histórica");

                if (sale is not null)
                {
                    DateOnly month = new DateOnly(today.Year, today.Month, 1).AddMonths(-monthsBack);
                    await BackdateSaleAsync(sale.Id, BusinessTime.StartOfDayUtc(month.AddDays(day - 1)).AddHours(15), cancellationToken);
                }
            }
        }
    }

    /// <summary>Lleva la venta, su factura, sus pagos y sus movimientos a la fecha indicada.</summary>
    private async Task BackdateSaleAsync(int saleId, DateTime date, CancellationToken cancellationToken)
    {
        PB_VENTA sale = await _context.Ventas
            .Include(item => item.Pagos).ThenInclude(payment => payment.MovimientoCaja)
            .Include(item => item.Factura)
            .SingleAsync(item => item.VEN_Venta == saleId, cancellationToken);

        sale.VEN_Fecha = date;
        sale.VEN_FechaCreacion = date;

        if (sale.Factura is { } invoice)
        {
            invoice.FAC_FechaEmision = date;
            invoice.FAC_FechaCertificacion = date;
        }

        foreach (PB_VENTA_PAGO payment in sale.Pagos)
        {
            payment.VPA_Fecha = date;

            if (payment.MovimientoCaja is { } movement)
            {
                movement.MCA_Fecha = date;
            }
        }

        await _context.MovimientosInventario
            .Where(item => item.TipoMovimiento.TMI_Codigo == SaleService.SaleExitType && item.MOV_DocumentoReferencia == sale.VEN_Numero)
            .ForEachAsync(item => item.MOV_Fecha = date, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    // ───────────────────────────── Traslados ─────────────────────────────

    private async Task SeedTransfersAsync(int adminId, int xela, CancellationToken cancellationToken)
    {
        int main = await _context.Almacenes
            .Where(item => item.ALM_Codigo == OrganizationSeeder.MainWarehouseCode)
            .Select(item => item.ALM_Almacen)
            .SingleAsync(cancellationToken);

        List<(int Id, decimal Price, decimal Tax)> books = await BooksWithStockAsync(6, cancellationToken);

        if (books.Count < 4)
        {
            return;
        }

        Require(await _transfers.CreateAsync(new SaveTransferRequest
        {
            SourceWarehouseId = main,
            TargetWarehouseId = xela,
            Notes = "Surtido inicial de la sucursal Xela.",
            Lines = books.Take(3).Select(book => new TransferLineRequest { BookId = book.Id, Quantity = 4 }).ToList()
        }, adminId, true, cancellationToken), "traslado inicial a Xela");

        Require(await _transfers.CreateAsync(new SaveTransferRequest
        {
            SourceWarehouseId = main,
            TargetWarehouseId = xela,
            Notes = "Reposición por demanda de la temporada escolar.",
            Lines = [new TransferLineRequest { BookId = books[3].Id, Quantity = 2 }]
        }, adminId, true, cancellationToken), "reposición a Xela");

        MovementResponse? mistaken = Require(await _transfers.CreateAsync(new SaveTransferRequest
        {
            SourceWarehouseId = main,
            TargetWarehouseId = xela,
            Notes = "Envío registrado por error.",
            Lines = [new TransferLineRequest { BookId = books[0].Id, Quantity = 1 }]
        }, adminId, true, cancellationToken), "traslado por error");

        if (mistaken is not null)
        {
            Require(await _transfers.AnnulAsync(mistaken.Id, new AnnulTransferRequest { Reason = "Se registró dos veces el mismo envío." },
                adminId, true, cancellationToken), "anulación de traslado");
        }
    }

    // ───────────────────────────── Devoluciones de clientes ─────────────────────────────

    private async Task SeedCustomerReturnsAsync(int adminId, Dictionary<string, PaymentMethodResponse> methods, CancellationToken cancellationToken)
    {
        ReturnCatalogsResponse catalogs = await _returns.GetCatalogsAsync(cancellationToken);
        int Reason(string code) => catalogs.Reasons.Single(reason => reason.Code == code).Id;

        DateTime recent = DateTime.UtcNow.AddDays(-20);

        // Ventas recientes (dentro del plazo de devolución) con unidades por devolver.
        var candidates = await _context.Ventas
            .AsNoTracking()
            .Where(sale => !sale.VEN_Anulada && sale.VEN_Fecha >= recent && sale.Detalles.Any(line => line.VDE_CantidadDevuelta < line.VDE_Cantidad))
            .OrderBy(sale => sale.VEN_Venta)
            .Select(sale => new
            {
                sale.VEN_Venta,
                Type = sale.TipoVenta.TVE_Codigo,
                Balance = sale.VEN_Total - sale.VEN_TotalPagado,
                Card = sale.Pagos.Any(payment => !payment.VPA_Anulado && payment.MetodoPago.MPA_Codigo == PaymentMethodCode.Card),
                Line = sale.Detalles.OrderBy(line => line.VDE_NumeroLinea).Select(line => line.VDE_VentaDetalle).First()
            })
            .ToListAsync(cancellationToken);

        var cash = candidates.FirstOrDefault(sale => sale.Type == SaleType.Cash && sale.Balance == 0 && !sale.Card);
        var credit = candidates.FirstOrDefault(sale => sale.Type == SaleType.Credit && sale.Balance > 0);
        var card = candidates.FirstOrDefault(sale => sale.Card && sale.VEN_Venta != cash?.VEN_Venta);

        if (cash is not null)
        {
            await ReturnAsync(adminId, cash.VEN_Venta, cash.Line, Reason("INCORRECTO"),
                "El cliente pidió otra edición del título.", null, cancellationToken);
        }

        if (credit is not null)
        {
            await ReturnAsync(adminId, credit.VEN_Venta, credit.Line, Reason("CAMBIO"),
                "Cambio autorizado por el supervisor; se abona al saldo.", null, cancellationToken);
        }

        if (card is not null)
        {
            await ReturnAsync(adminId, card.VEN_Venta, card.Line, Reason("DANIO"),
                "Ejemplar con la portada rota; se da de baja.", methods[PaymentMethodCode.Card].Id, cancellationToken);
        }
    }

    private async Task ReturnAsync(int adminId, int sale, int line, int reason, string notes, int? refundMethod, CancellationToken cancellationToken)
    {
        Require(await _returns.CreateAsync(new SaveReturnRequest
        {
            SaleId = sale,
            RefundMethodId = refundMethod,
            RefundReference = refundMethod is null ? null : "Reversión de cargo 55821",
            Lines = [new ReturnLineRequest { SaleLineId = line, Quantity = 1, ReasonId = reason, Notes = notes }]
        }, adminId, cancellationToken), "devolución de cliente");
    }

    // ───────────────────────────── Devolución al proveedor ─────────────────────────────

    private async Task SeedSupplierReturnAsync(int adminId, CancellationToken cancellationToken)
    {
        int[] receptions = await _context.RecepcionesCompra
            .Where(item => item.EstadoRecepcionCompra.ERC_Codigo == ReceptionStatus.Processed)
            .OrderByDescending(item => item.REC_RecepcionCompra)
            .Select(item => item.REC_RecepcionCompra)
            .Take(5)
            .ToArrayAsync(cancellationToken);

        foreach (int reception in receptions)
        {
            ReturnableReceptionResponse? returnable = await _supplierReturns.GetReturnableAsync(reception, cancellationToken);
            ReturnableLineResponse? line = returnable?.Lines.FirstOrDefault(item => item.Returnable >= 3);

            if (line is null)
            {
                continue;
            }

            Require(await _supplierReturns.CreateAsync(new SaveSupplierReturnRequest
            {
                ReceptionId = reception,
                Reason = "Ejemplares con páginas mal encuadernadas; el proveedor emitirá nota de crédito.",
                Lines = [new SupplierReturnLineRequest { ReceptionLineId = line.ReceptionLineId, Quantity = 2 }]
            }, adminId, cancellationToken), "devolución al proveedor");

            return;
        }
    }

    // ───────────────────────────── Alertas ─────────────────────────────

    /// <summary>Un crédito vencido y la caja del administrador abierta desde ayer.</summary>
    private async Task SeedAlertsAsync(int session, CancellationToken cancellationToken)
    {
        PB_VENTA? credit = await _context.Ventas
            .Where(sale => !sale.VEN_Anulada && sale.VEN_TotalPagado < sale.VEN_Total && sale.VEN_FechaVencimientoCredito != null)
            .OrderBy(sale => sale.VEN_Venta)
            .FirstOrDefaultAsync(cancellationToken);

        if (credit is not null)
        {
            credit.VEN_FechaVencimientoCredito = BusinessTime.Today.AddDays(-12);
        }

        var open = await _context.AperturasCaja.SingleAsync(item => item.ACA_AperturaCaja == session, cancellationToken);
        open.ACA_FechaApertura = open.ACA_FechaApertura.AddDays(-1);

        await _context.SaveChangesAsync(cancellationToken);
    }

    // ───────────────────────────── Apoyo ─────────────────────────────

    /// <summary>Libros con precio en la lista predeterminada y existencia libre en el almacén principal.</summary>
    private async Task<List<(int Id, decimal Price, decimal Tax)>> BooksWithStockAsync(int count, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        var books = await _context.Existencias
            .AsNoTracking()
            .Where(stock => stock.Almacen.ALM_Codigo == OrganizationSeeder.MainWarehouseCode &&
                            stock.EXI_CantidadDisponible - stock.EXI_CantidadReservada >= 12 &&
                            stock.Libro.LIB_Activo && stock.Libro.LIB_PermiteVenta)
            .OrderBy(stock => stock.Libro.LIB_Codigo)
            .Select(stock => new
            {
                stock.EXI_LibroId,
                Tax = stock.Libro.Impuesto.IMP_Porcentaje,
                Price = stock.Libro.Precios
                    .Where(price => price.LIP_Activo && price.ListaPrecio.LPR_EsPredeterminada &&
                                    price.LIP_FechaInicio <= now && (price.LIP_FechaFin == null || price.LIP_FechaFin > now))
                    .Select(price => (decimal?)price.LIP_Precio)
                    .FirstOrDefault()
            })
            .Where(item => item.Price != null)
            .Take(count)
            .ToListAsync(cancellationToken);

        return books.Select(item => (item.EXI_LibroId, item.Price!.Value, item.Tax)).ToList();
    }

    private T? Require<T>(OperationResult<T> result, string what) where T : class
    {
        if (result.Succeeded)
        {
            return result.Data;
        }

        _logger.LogWarning("Demostración de la entrega final ({What}) no registrada: {Errors}", what, string.Join(" ", result.Errors));
        return null;
    }
}
