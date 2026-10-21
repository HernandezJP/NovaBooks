using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Customers;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Cash;
using TikalLibros.Infrastructure.Data.Identity;
using TikalLibros.Infrastructure.Services.Sales;

namespace TikalLibros.Infrastructure.Data.Seed;

/// <summary>
/// Datos de demostración de pedidos, ventas y caja (solo en desarrollo):
/// clientes con y sin crédito, un cajero y una supervisora con usuario,
/// cajas con aperturas abiertas y cerradas (con arqueo), ventas de
/// mostrador con varios métodos de pago, pedidos en todos los estados
/// (incluido uno por aprobar), ventas al crédito con abonos y una venta
/// anulada. Usa los servicios de la aplicación para que inventario, caja,
/// numeración y auditoría queden consistentes. Las operaciones se crean
/// solo si todavía no hay pedidos ni ventas; clientes y usuarios se agregan
/// si faltan. Los usuarios se crean sin contraseña: para entrar con ellos,
/// un administrador debe restablecerla desde Usuarios.
/// </summary>
public sealed class DemoSalesSeeder
{
    public const string CashierUserName = "jramirez";
    public const string SupervisorUserName = "mgarcia";
    public const string SecondCashRegisterCode = "CAJA-02";

    private static readonly SaveCustomerRequest[] Customers =
    [
        Natural("Ana", "Sofía", "Morales", "Castillo", "4815162K", "ana.morales@correo.test", 0m, 0),
        Natural("Diego", "Alejandro", "López", "Pérez", "9876543", "diego.lopez@correo.test", 0m, 0),
        Natural("Gabriela", "Fernanda", "Ruiz", "Méndez", "5612347K", "gabriela.ruiz@correo.test", 1500m, 30),
        Natural("Mario", "Roberto", "Chávez", "Soto", "7345128", "mario.chavez@correo.test", 0m, 0),
        Legal("Colegio Bilingüe El Quetzal, S.A.", "Colegio El Quetzal", "6712345K", "compras@colegioquetzal.test", 8000m, 30),
        Legal("Biblioteca Municipal de Antigua Guatemala", "Biblioteca de Antigua", "3218765", "biblioteca@antigua.test", 5000m, 45),
        Legal("Librería Universitaria del Valle, S.A.", "Librería del Valle", "8456123", "pedidos@libreriadelvalle.test", 12000m, 60),
        Legal("Academia de Idiomas Maya, S.A.", "Academia Maya", "2398451", "info@academiamaya.test", 0m, 0)
    ];

    private readonly AppDbContext _context;
    private readonly ICustomerService _customers;
    private readonly ISupplierService _suppliers;
    private readonly IReceptionService _receptions;
    private readonly ICashService _cash;
    private readonly IOrderService _orders;
    private readonly ISaleService _sales;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoSalesSeeder> _logger;

    public DemoSalesSeeder(
        AppDbContext context,
        ICustomerService customers,
        ISupplierService suppliers,
        IReceptionService receptions,
        ICashService cash,
        IOrderService orders,
        ISaleService sales,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<DemoSalesSeeder> logger)
    {
        _context = context;
        _customers = customers;
        _suppliers = suppliers;
        _receptions = receptions;
        _cash = cash;
        _orders = orders;
        _sales = sales;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        string? adminName = _configuration["IdentitySeed:Administrator:UserName"];
        ApplicationUser? admin = string.IsNullOrWhiteSpace(adminName) ? null : await _userManager.FindByNameAsync(adminName);

        if (admin?.USU_EmpleadoId is null)
        {
            _logger.LogWarning("No se cargaron ventas de demostración: el administrador no tiene empleado.");
            return;
        }

        await SeedCustomersAsync(cancellationToken);
        int? cashier = await EnsureUserAsync(CashierUserName, "José", "Ramírez", "Cajero", cancellationToken);
        int? supervisor = await EnsureUserAsync(SupervisorUserName, "María", "García", "Supervisor", cancellationToken);

        if (cashier is null || supervisor is null ||
            await _context.PedidosCliente.AnyAsync(cancellationToken) ||
            await _context.Ventas.AnyAsync(cancellationToken))
        {
            return;
        }

        await EnsureSecondCashRegisterAsync(cancellationToken);
        await RestockAsync(admin.Id, cancellationToken);

        List<DemoBook> books = await BooksAsync(cancellationToken);

        if (books.Count < 8)
        {
            _logger.LogWarning("No se cargaron ventas de demostración: faltan libros con precio y existencia.");
            return;
        }

        Dictionary<string, int> customers = (await _customers.GetPagedAsync(new CustomerQuery { IsActive = true, PageSize = 100 }, cancellationToken))
            .Items.Where(item => item.Nit is not null)
            .ToDictionary(item => item.Nit!, item => item.Id);

        Dictionary<string, PaymentMethodResponse> methods = (await _sales.GetCatalogsAsync(cancellationToken))
            .PaymentMethods.ToDictionary(item => item.Code);

        var context = new Run(books, customers, methods, admin.Id, cashier.Value, supervisor.Value);

        await SeedCashierShiftAsync(context, cancellationToken);
        await SeedSupervisedOrdersAsync(context, cancellationToken);

        _logger.LogInformation("Datos de demostración de pedidos, ventas y caja cargados.");
    }

    // ───────────────────────────── Turno del cajero ─────────────────────────────

    /// <summary>
    /// Primer turno cerrado con arqueo (faltan Q5), segundo turno abierto.
    /// Ventas de mostrador con efectivo, tarjeta y transferencia, una
    /// anulada, y pedidos facturado, reservado, cancelado y en borrador.
    /// </summary>
    private async Task SeedCashierShiftAsync(Run run, CancellationToken cancellationToken)
    {
        int mainRegister = await RegisterIdAsync(OrganizationSeeder.MainCashRegisterCode, cancellationToken);
        CashSessionResponse? shift = Require(await _cash.OpenAsync(
            new OpenCashSessionRequest { CashRegisterId = mainRegister, InitialAmount = 500m, Notes = "Fondo de cambio del turno de la mañana." },
            run.Cashier, cancellationToken), "apertura de caja del cajero");

        if (shift is null)
        {
            return;
        }

        Require(await _cash.RegisterMovementAsync(shift.Id, new CashMovementRequest
        {
            TypeCode = CashMovementTypes.ManualIncome,
            Amount = 200m,
            Concept = "Fondo adicional de cambio en monedas",
            Reference = "VALE-0012"
        }, run.Cashier, new CashPermissionSet(true, false, false), cancellationToken), "ingreso de caja");

        SalesPermissionSet cashier = new(false, false, false);

        // Ventas de mostrador.
        await SellAsync(run, null, cashier, run.Cashier, [Line(run, 0, 1), Line(run, 1, 1)], received: 500m, cancellationToken: cancellationToken);
        await SellAsync(run, run.Customers["9876543"], cashier, run.Cashier, [Line(run, 2, 2)], method: PaymentMethodCode.Card, cancellationToken: cancellationToken);
        await SellAsync(run, run.Customers["2398451"], cashier, run.Cashier, [Line(run, 3, 3), Line(run, 4, 1)], method: PaymentMethodCode.Transfer, cancellationToken: cancellationToken);

        SaleResponse? annulled = await SellAsync(run, null, cashier, run.Cashier, [Line(run, 5, 1)], cancellationToken: cancellationToken);

        if (annulled is not null)
        {
            Require(await _sales.AnnulAsync(annulled.Id,
                new AnnulSaleRequest { Reason = "El cliente cambió de opinión antes de salir de la tienda." },
                run.Admin, cancellationToken), "anulación de venta");
        }

        // Pedidos del cajero: facturado, reservado, cancelado y en borrador.
        OrderResponse? invoiced = await OrderAsync(run, run.Customers["4815162K"], cashier, run.Cashier, [Line(run, 6, 2)], confirm: true, cancellationToken: cancellationToken);

        if (invoiced is not null)
        {
            Require(await _sales.InvoiceOrderAsync(invoiced.Id, new InvoiceOrderRequest
            {
                Payments = [CashPayment(run, invoiced.Total, Math.Ceiling(invoiced.Total / 100m) * 100m)],
                Notes = "Pasó a recoger su pedido."
            }, run.Cashier, cashier, cancellationToken), "facturación de pedido");
        }

        await OrderAsync(run, run.Customers["7345128"], cashier, run.Cashier, [Line(run, 7, 1), Line(run, 0, 1)], confirm: true,
            notes: "Recoge el sábado.", cancellationToken: cancellationToken);

        OrderResponse? cancelled = await OrderAsync(run, run.Customers["9876543"], cashier, run.Cashier, [Line(run, 1, 1)], confirm: true, cancellationToken: cancellationToken);

        if (cancelled is not null)
        {
            Require(await _orders.CancelAsync(cancelled.Id,
                new OrderReasonRequest { Reason = "El cliente encontró el libro en otra presentación." },
                run.Cashier, cashier, cancellationToken), "cancelación de pedido");
        }

        await OrderAsync(run, run.Customers["4815162K"], cashier, run.Cashier, [Line(run, 2, 1)], confirm: false,
            notes: "Cotización; confirmará por teléfono.", cancellationToken: cancellationToken);

        // Cierre con un faltante explicado y nuevo turno abierto con una venta.
        CashSessionResponse current = (await _cash.GetSessionAsync(shift.Id, run.Cashier, cancellationToken))!;

        Require(await _cash.CloseAsync(shift.Id, new CloseCashSessionRequest
        {
            CountedCash = current.ExpectedCash - 5m,
            Notes = "Faltaron Q5.00 en monedas al cuadrar el fondo de cambio."
        }, run.Cashier, new CashPermissionSet(true, false, false), cancellationToken), "cierre de caja del cajero");

        CashSessionResponse? afternoon = Require(await _cash.OpenAsync(
            new OpenCashSessionRequest { CashRegisterId = mainRegister, InitialAmount = 300m, Notes = "Turno de la tarde." },
            run.Cashier, cancellationToken), "segunda apertura del cajero");

        if (afternoon is not null)
        {
            await SellAsync(run, null, cashier, run.Cashier, [Line(run, 3, 1)], received: 200m, cancellationToken: cancellationToken);
        }
    }

    // ─────────────────────── Pedidos de la supervisora ───────────────────────

    /// <summary>
    /// Pedidos con descuento alto (uno queda por aprobar, otro aprobado y
    /// facturado, otro rechazado), un pedido al crédito facturado con abono
    /// inicial y una venta al crédito con abono posterior. El administrador
    /// aprueba y cobra en la caja 2, que queda cerrada.
    /// </summary>
    private async Task SeedSupervisedOrdersAsync(Run run, CancellationToken cancellationToken)
    {
        SalesPermissionSet supervisor = new(true, true, true);

        // Por aprobar: queda para demostrar la aprobación.
        await OrderAsync(run, run.Customers["3218765"], supervisor, run.Supervisor, [Line(run, 4, 4, 15m), Line(run, 5, 2, 15m)], confirm: true,
            notes: "Donación parcial para la biblioteca: descuento especial.", cancellationToken: cancellationToken);

        OrderResponse? approved = await OrderAsync(run, run.Customers["6712345K"], supervisor, run.Supervisor,
            [Line(run, 0, 5, 20m), Line(run, 6, 5, 20m)], confirm: true, notes: "Lectura obligatoria de segundo básico.", cancellationToken: cancellationToken);

        OrderResponse? rejected = await OrderAsync(run, run.Customers["8456123"], supervisor, run.Supervisor,
            [Line(run, 2, 3, 35m)], confirm: true, cancellationToken: cancellationToken);

        int secondRegister = await RegisterIdAsync(SecondCashRegisterCode, cancellationToken);
        CashSessionResponse? adminShift = Require(await _cash.OpenAsync(
            new OpenCashSessionRequest { CashRegisterId = secondRegister, InitialAmount = 250m },
            run.Admin, cancellationToken), "apertura de caja del administrador");

        if (approved is not null)
        {
            OrderResponse? reserved = Require(await _orders.ApproveAsync(approved.Id,
                new ApproveOrderRequest { Comment = "Cliente frecuente: compra anual para su biblioteca escolar." },
                run.Admin, supervisor, cancellationToken), "aprobación de pedido");

            if (reserved is not null && adminShift is not null)
            {
                Require(await _sales.InvoiceOrderAsync(reserved.Id, new InvoiceOrderRequest
                {
                    Payments = [CardPayment(run, reserved.Total, "8842")]
                }, run.Admin, supervisor, cancellationToken), "facturación de pedido aprobado");
            }
        }

        if (rejected is not null)
        {
            Require(await _orders.RejectAsync(rejected.Id,
                new OrderReasonRequest { Reason = "Un descuento de 35 % está fuera de la política comercial." },
                run.Admin, supervisor, cancellationToken), "rechazo de pedido");
        }

        // Pedido al crédito facturado con abono inicial del 30 %.
        OrderResponse? credit = await OrderAsync(run, run.Customers["5612347K"], supervisor, run.Supervisor, [Line(run, 7, 2), Line(run, 3, 1)],
            confirm: true, saleType: SaleType.Credit, cancellationToken: cancellationToken);

        if (credit is not null && adminShift is not null)
        {
            decimal downPayment = decimal.Round(credit.Total * 0.3m, 2);

            Require(await _sales.InvoiceOrderAsync(credit.Id, new InvoiceOrderRequest
            {
                Payments = [CashPayment(run, downPayment, downPayment)]
            }, run.Admin, supervisor, cancellationToken), "facturación al crédito");
        }

        // Venta al crédito sin abono y un abono posterior por transferencia.
        if (adminShift is not null)
        {
            SaleResponse? creditSale = await SellAsync(run, run.Customers["8456123"], supervisor, run.Admin, [Line(run, 1, 3), Line(run, 4, 2)],
                saleType: SaleType.Credit, cancellationToken: cancellationToken);

            if (creditSale is not null)
            {
                Require(await _sales.AddPaymentsAsync(creditSale.Id, new AddPaymentsRequest
                {
                    Payments = [TransferPayment(run, decimal.Round(creditSale.Total / 2m, 2), "TRF-20481")]
                }, run.Admin, cancellationToken), "abono a venta al crédito");
            }

            Require(await _cash.RegisterMovementAsync(adminShift.Id, new CashMovementRequest
            {
                TypeCode = CashMovementTypes.Expense,
                Amount = 45m,
                Concept = "Compra de bolsas de papel para empaque",
                Reference = "FAC-77810"
            }, run.Admin, new CashPermissionSet(true, true, true), cancellationToken), "gasto de caja");

            CashSessionResponse current = (await _cash.GetSessionAsync(adminShift.Id, run.Admin, cancellationToken))!;

            Require(await _cash.CloseAsync(adminShift.Id, new CloseCashSessionRequest { CountedCash = current.ExpectedCash },
                run.Admin, new CashPermissionSet(true, true, true), cancellationToken), "cierre de caja del administrador");
        }
    }

    // ───────────────────────────── Ayudantes ─────────────────────────────

    private sealed record DemoBook(int Id, decimal Price, decimal Tax);

    private sealed record Run(
        List<DemoBook> Books,
        Dictionary<string, int> Customers,
        Dictionary<string, PaymentMethodResponse> Methods,
        int Admin,
        int Cashier,
        int Supervisor);

    private static OrderLineRequest Line(Run run, int book, int quantity, decimal discount = 0m) =>
        new() { BookId = run.Books[book].Id, Quantity = quantity, DiscountPercent = discount };

    private static decimal Total(Run run, IEnumerable<OrderLineRequest> lines) =>
        lines.Sum(line =>
        {
            DemoBook book = run.Books.Single(item => item.Id == line.BookId);
            return SaleLineAmounts.Calculate(line.Quantity!.Value, book.Price, line.DiscountPercent, book.Tax).Total;
        });

    private static PaymentRequest CashPayment(Run run, decimal amount, decimal received) =>
        new() { PaymentMethodId = run.Methods[PaymentMethodCode.Cash].Id, Amount = amount, Received = received };

    private static PaymentRequest CardPayment(Run run, decimal amount, string lastFour) =>
        new() { PaymentMethodId = run.Methods[PaymentMethodCode.Card].Id, Amount = amount, Authorization = "AUT" + lastFour, LastFourDigits = lastFour };

    private static PaymentRequest TransferPayment(Run run, decimal amount, string reference) =>
        new() { PaymentMethodId = run.Methods[PaymentMethodCode.Transfer].Id, Amount = amount, Reference = reference };

    private async Task<SaleResponse?> SellAsync(
        Run run,
        int? customer,
        SalesPermissionSet permissions,
        int userId,
        List<OrderLineRequest> lines,
        string method = PaymentMethodCode.Cash,
        decimal? received = null,
        string saleType = SaleType.Cash,
        CancellationToken cancellationToken = default)
    {
        decimal total = Total(run, lines);

        List<PaymentRequest> payments = saleType == SaleType.Credit ? [] : method switch
        {
            PaymentMethodCode.Card => [CardPayment(run, total, "4242")],
            PaymentMethodCode.Transfer => [TransferPayment(run, total, $"TRF-{total * 100:0}")],
            _ => [CashPayment(run, total, Math.Max(received ?? total, total))]
        };

        return Require(await _sales.SellAsync(new DirectSaleRequest
        {
            CustomerId = customer,
            SaleType = saleType,
            Lines = lines,
            Payments = payments
        }, userId, permissions, cancellationToken), "venta de mostrador");
    }

    private async Task<OrderResponse?> OrderAsync(
        Run run,
        int customer,
        SalesPermissionSet permissions,
        int userId,
        List<OrderLineRequest> lines,
        bool confirm,
        string saleType = SaleType.Cash,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        OrderResponse? order = Require(await _orders.CreateAsync(new SaveOrderRequest
        {
            CustomerId = customer,
            SaleType = saleType,
            Notes = notes,
            Lines = lines
        }, userId, permissions, cancellationToken), "pedido");

        return order is null || !confirm
            ? order
            : Require(await _orders.ConfirmAsync(order.Id, userId, permissions, cancellationToken), "confirmación de pedido");
    }

    private async Task SeedCustomersAsync(CancellationToken cancellationToken)
    {
        foreach (SaveCustomerRequest customer in Customers)
        {
            OperationResult<CustomerResponse> result = await _customers.CreateAsync(customer, cancellationToken);

            // Un conflicto indica que el NIT ya existe: se deja como está.
            if (!result.Succeeded && result.ErrorType != OperationErrorType.Conflict)
            {
                _logger.LogWarning("Cliente de demostración no creado: {Errors}", string.Join(" ", result.Errors));
            }
        }
    }

    /// <summary>Usuario sin contraseña para un empleado de demostración.</summary>
    private async Task<int?> EnsureUserAsync(string userName, string firstName, string lastName, string role, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByNameAsync(userName);

        if (user is not null)
        {
            return user.USU_EmpleadoId is null ? null : user.Id;
        }

        int? employee = await _context.Empleados
            .Where(item => item.EMP_Activo &&
                           item.Persona.PersonaNatural!.PNA_PrimerNombre == firstName &&
                           item.Persona.PersonaNatural.PNA_PrimerApellido == lastName)
            .Select(item => (int?)item.EMP_Empleado)
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null || await _userManager.Users.AnyAsync(item => item.USU_EmpleadoId == employee, cancellationToken))
        {
            _logger.LogWarning("No se creó el usuario {UserName}: falta su empleado de demostración o ya tiene usuario.", userName);
            return null;
        }

        user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@tikallibros.test",
            EmailConfirmed = true,
            LockoutEnabled = true,
            USU_EmpleadoId = employee,
            USU_Activo = true,
            USU_DebeCambiarPassword = true,
            USU_FechaCreacion = DateTime.UtcNow
        };

        IdentityResult created = await _userManager.CreateAsync(user);

        if (!created.Succeeded || !(await _userManager.AddToRoleAsync(user, role)).Succeeded)
        {
            _logger.LogWarning("No se creó el usuario de demostración {UserName}.", userName);
            return null;
        }

        return user.Id;
    }

    private async Task EnsureSecondCashRegisterAsync(CancellationToken cancellationToken)
    {
        if (await _context.Cajas.AnyAsync(item => item.CAJ_Codigo == SecondCashRegisterCode, cancellationToken))
        {
            return;
        }

        int branch = await _context.Sucursales
            .Where(item => item.SUC_Codigo == OrganizationSeeder.MainBranchCode)
            .Select(item => item.SUC_Sucursal)
            .SingleAsync(cancellationToken);

        _context.Cajas.Add(new PB_CAJA
        {
            CAJ_SucursalId = branch,
            CAJ_Codigo = SecondCashRegisterCode,
            CAJ_Nombre = "Caja de mostrador 2",
            CAJ_Descripcion = "Caja de apoyo para temporada escolar.",
            CAJ_Activo = true
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Compra directa de los títulos de demostración para que haya existencia suficiente.</summary>
    private async Task RestockAsync(int adminId, CancellationToken cancellationToken)
    {
        int? supplier = (await _suppliers.GetPagedAsync(null, true, null, 1, 1, cancellationToken)).Items.FirstOrDefault()?.Id;

        var books = await _context.Libros
            .Where(book => book.LIB_Activo && book.LIB_PermiteVenta && book.LIB_ISBN13 != null && book.LIB_ISBN13.StartsWith("97899922"))
            .OrderBy(book => book.LIB_Codigo)
            .Select(book => new { book.LIB_Libro, book.LIB_CostoReferencia })
            .Take(10)
            .ToListAsync(cancellationToken);

        if (supplier is null || books.Count == 0)
        {
            return;
        }

        Require(await _receptions.RegisterDirectPurchaseAsync(new DirectPurchaseRequest
        {
            Order = new SavePurchaseOrderRequest
            {
                SupplierId = supplier,
                Lines = books
                    .Select(book => new PurchaseOrderLineRequest
                    {
                        BookId = book.LIB_Libro,
                        Quantity = 20,
                        UnitCost = book.LIB_CostoReferencia > 0 ? decimal.Round(book.LIB_CostoReferencia, 2) : 60m
                    })
                    .ToList()
            },
            SupplierDocumentSeries = "TEMP-ESC",
            SupplierDocumentNumber = "2026101",
            Notes = "Reposición para la temporada escolar."
        }, adminId, cancellationToken), "reposición de existencia");
    }

    /// <summary>Libros de demostración con precio en la lista general y existencia libre.</summary>
    private async Task<List<DemoBook>> BooksAsync(CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        return await _context.Existencias
            .AsNoTracking()
            .Where(stock => stock.EXI_CantidadDisponible - stock.EXI_CantidadReservada >= 10 &&
                            stock.Almacen.ALM_EsPrincipal &&
                            stock.Libro.LIB_Activo && stock.Libro.LIB_PermiteVenta)
            .Select(stock => new
            {
                stock.EXI_LibroId,
                stock.Libro.LIB_Codigo,
                Tax = stock.Libro.Impuesto.IMP_Porcentaje,
                Price = stock.Libro.Precios
                    .Where(price => price.LIP_Activo && price.ListaPrecio.LPR_EsPredeterminada &&
                                    price.LIP_FechaInicio <= now && (price.LIP_FechaFin == null || price.LIP_FechaFin > now))
                    .Select(price => (decimal?)price.LIP_Precio)
                    .FirstOrDefault()
            })
            .Where(item => item.Price != null)
            .OrderBy(item => item.LIB_Codigo)
            .Select(item => new DemoBook(item.EXI_LibroId, item.Price!.Value, item.Tax))
            .ToListAsync(cancellationToken);
    }

    private async Task<int> RegisterIdAsync(string code, CancellationToken cancellationToken) =>
        await _context.Cajas.Where(item => item.CAJ_Codigo == code).Select(item => item.CAJ_Caja).SingleAsync(cancellationToken);

    private T? Require<T>(OperationResult<T> result, string what) where T : class
    {
        if (result.Succeeded)
        {
            return result.Data;
        }

        _logger.LogWarning("Demostración de ventas ({What}) no registrada: {Errors}", what, string.Join(" ", result.Errors));
        return null;
    }

    private static SaveCustomerRequest Natural(
        string first, string middle, string last, string secondLast, string nit, string email, decimal credit, int days) => new()
    {
        PersonType = SaveCustomerRequest.NaturalPerson,
        FirstName = first,
        MiddleName = middle,
        LastName = last,
        SecondLastName = secondLast,
        Nit = nit,
        Email = email,
        CreditLimit = credit,
        CreditDays = days
    };

    private static SaveCustomerRequest Legal(
        string legalName, string tradeName, string nit, string email, decimal credit, int days) => new()
    {
        PersonType = SaveCustomerRequest.LegalPerson,
        LegalName = legalName,
        TradeName = tradeName,
        Nit = nit,
        Email = email,
        CreditLimit = credit,
        CreditDays = days
    };
}
