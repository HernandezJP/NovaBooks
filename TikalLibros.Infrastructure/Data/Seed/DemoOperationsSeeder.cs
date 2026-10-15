using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Employees;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Data.Identity;
using TikalLibros.Infrastructure.Services.Purchasing;

namespace TikalLibros.Infrastructure.Data.Seed;

/// <summary>
/// Datos de demostración de compras e inventario (solo en desarrollo):
/// proveedores, empleados sin usuario, órdenes en todos los estados,
/// recepciones, existencias con mínimos y máximos, alertas y ajustes.
/// Usa los servicios de la aplicación para que lotes, existencias,
/// movimientos y numeración queden consistentes. Las órdenes se crean solo
/// si todavía no hay ninguna; proveedores y empleados se agregan por NIT/DPI.
/// </summary>
public sealed class DemoOperationsSeeder
{
    private static readonly SaveSupplierRequest[] Suppliers =
    [
        Supplier("Distribuidora Quetzal Libros, S.A.", "Quetzal Libros", "4521873K", "ventas@quetzallibros.test", "22345678", 25000m, 30),
        Supplier("Editorial Lago de Atitlán, S.A.", "Lago de Atitlán", "6893412", "pedidos@lagoatitlan.test", "23456789", 15000m, 45),
        Supplier("Comercial Libros del Volcán, S.A.", "Libros del Volcán", "3376259", "compras@librosdelvolcan.test", "24567890", 0m, 0),
        Supplier("Importadora Letras de Centroamérica, S.A.", "Letras CA", "8812540K", "info@letrasca.test", "25678901", 40000m, 60),
        new SaveSupplierRequest
        {
            PersonType = SaveSupplierRequest.NaturalPerson,
            FirstName = "Carlos",
            MiddleName = "Alberto",
            LastName = "Méndez",
            SecondLastName = "Ruiz",
            Nit = "2945716",
            Email = "carlos.mendez@correo.test",
            Phone = "55123456",
            Notes = "Libros usados y ediciones antiguas."
        }
    ];

    private static readonly (string First, string? Middle, string Last, string? SecondLast, string Position, string Correlative, DateOnly Birth)[] Employees =
    [
        ("María", "José", "García", "López", "Gerente general", "31245678", new DateOnly(1982, 4, 12)),
        ("Luis", "Fernando", "Pérez", "Castillo", "Encargado de compras", "42356789", new DateOnly(1990, 9, 3)),
        ("Ana", "Lucía", "Hernández", "Morales", "Encargado de bodega", "53467891", new DateOnly(1995, 1, 27)),
        ("José", null, "Ramírez", "Ortiz", "Vendedor", "64578912", new DateOnly(1999, 11, 8))
    ];

    private readonly AppDbContext _context;
    private readonly ISupplierService _suppliers;
    private readonly IEmployeeService _employees;
    private readonly PurchaseOrderService _orders;
    private readonly IReceptionService _receptions;
    private readonly IInventoryService _inventory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoOperationsSeeder> _logger;

    public DemoOperationsSeeder(
        AppDbContext context,
        ISupplierService suppliers,
        IEmployeeService employees,
        PurchaseOrderService orders,
        IReceptionService receptions,
        IInventoryService inventory,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<DemoOperationsSeeder> logger)
    {
        _context = context;
        _suppliers = suppliers;
        _employees = employees;
        _orders = orders;
        _receptions = receptions;
        _inventory = inventory;
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
            _logger.LogWarning("No se cargaron compras de demostración: el administrador no tiene empleado.");
            return;
        }

        await SeedSuppliersAsync(cancellationToken);
        await SeedEmployeesAsync(cancellationToken);

        if (await _context.OrdenesCompra.AnyAsync(cancellationToken))
        {
            return;
        }

        List<DemoBook> books = await _context.Libros
            .AsNoTracking()
            .Where(book => book.LIB_Activo && book.LIB_ISBN13 != null && book.LIB_ISBN13.StartsWith("97899922"))
            .OrderBy(book => book.LIB_Codigo)
            .Select(book => new DemoBook(
                book.LIB_Libro,
                _context.LibrosPrecios
                    .Where(price => price.LIP_LibroId == book.LIB_Libro && price.LIP_Activo)
                    .Select(price => (decimal?)price.LIP_Precio)
                    .FirstOrDefault() ?? 150m))
            .ToListAsync(cancellationToken);

        if (books.Count < 16)
        {
            _logger.LogWarning("No se cargaron compras de demostración: faltan los libros de demostración.");
            return;
        }

        Dictionary<string, int> suppliers = (await _suppliers.GetPagedAsync(null, true, null, 1, 100, cancellationToken))
            .Items.ToDictionary(item => item.Nit ?? string.Empty, item => item.Id);

        int userId = admin.Id;

        await SeedOrdersAsync(books, suppliers, userId, cancellationToken);
        await SeedStockLevelsAsync(books, cancellationToken);
        await SeedAdjustmentsAsync(books, userId, cancellationToken);

        _logger.LogInformation("Datos de demostración de compras e inventario cargados.");
    }

    private async Task SeedSuppliersAsync(CancellationToken cancellationToken)
    {
        int phoneType = (await _suppliers.GetCatalogsAsync(cancellationToken)).PhoneTypes.First().Id;

        foreach (SaveSupplierRequest supplier in Suppliers)
        {
            supplier.PhoneTypeId = supplier.Phone is null ? null : phoneType;


            OperationResult<SupplierResponse> result = await _suppliers.CreateAsync(supplier, cancellationToken);

            // Un conflicto indica que el NIT ya existe: se deja como está.
            if (!result.Succeeded && result.ErrorType != OperationErrorType.Conflict)
            {
                _logger.LogWarning("Proveedor de demostración no creado: {Errors}", string.Join(" ", result.Errors));
            }
        }
    }

    private async Task SeedEmployeesAsync(CancellationToken cancellationToken)
    {
        EmployeeCatalogsResponse catalogs = await _employees.GetCatalogsAsync(cancellationToken);
        int branch = catalogs.Branches.First().Id;

        foreach (var employee in Employees)
        {
            OperationResult<EmployeeResponse> result = await _employees.CreateAsync(new SaveEmployeeRequest
            {
                FirstName = employee.First,
                MiddleName = employee.Middle,
                LastName = employee.Last,
                SecondLastName = employee.SecondLast,
                BirthDate = employee.Birth,
                Dpi = Dpi(employee.Correlative),
                Email = $"{employee.First.ToLowerInvariant()}.{employee.Last.ToLowerInvariant()}@tikallibros.test"
                    .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u"),
                PositionId = catalogs.Positions.Single(position => position.Name == employee.Position).Id,
                BranchId = branch,
                HireDate = DateOnly.FromDateTime(DateTime.Today).AddMonths(-6)
            }, cancellationToken);

            if (!result.Succeeded && result.ErrorType != OperationErrorType.Conflict)
            {
                _logger.LogWarning("Empleado de demostración no creado: {Errors}", string.Join(" ", result.Errors));
            }
        }
    }

    private async Task SeedOrdersAsync(
        List<DemoBook> books,
        Dictionary<string, int> suppliers,
        int userId,
        CancellationToken cancellationToken)
    {
        // 1. Recibida completamente, con factura.
        PurchaseOrderResponse? received = await IssueAsync(Order(suppliers["4521873K"], books[0..5], 24, 5m), userId, cancellationToken);
        if (received is not null)
        {
            await ReceiveAsync(received, line => ((int)line.Pending, 0, null), "A1B2C3D4", "1500231", userId, cancellationToken);
        }

        // 2. Recibida parcialmente, con unidades rechazadas.
        PurchaseOrderResponse? partial = await IssueAsync(Order(suppliers["6893412"], books[5..9], 15, 0m), userId, cancellationToken);
        if (partial is not null)
        {
            await ReceiveAsync(partial, line => (8, 2, "Ejemplares con la portada dañada en el envío."), "LA-2026", "88421", userId, cancellationToken);
        }

        // 3. Emitida, pendiente de recibir.
        await IssueAsync(Order(suppliers["8812540K"], books[9..12], 10, 10m, daysToDeliver: 10), userId, cancellationToken);

        // 4. En borrador.
        await CreateAsync(Order(suppliers["3376259"], books[12..14], 6, 0m, daysToDeliver: 15), userId, cancellationToken);

        // 5. Cancelada.
        PurchaseOrderResponse? cancelled = await IssueAsync(Order(suppliers["2945716"], books[14..15], 4, 0m), userId, cancellationToken);
        if (cancelled is not null)
        {
            await _orders.CancelAsync(cancelled.Id,
                new CancelPurchaseOrderRequest { Reason = "El proveedor ya no tiene estos títulos en existencia." },
                userId, cancellationToken);
        }

        // 6. Compra directa (registrada y recibida en un paso).
        OperationResult<ReceptionResponse> direct = await _receptions.RegisterDirectPurchaseAsync(
            new DirectPurchaseRequest
            {
                Order = Order(suppliers["4521873K"], [books[2], books[15]], 6, 0m),
                SupplierDocumentSeries = "A1B2C3D4",
                SupplierDocumentNumber = "1500298",
                Notes = "Reposición urgente de títulos de alta rotación."
            },
            userId,
            cancellationToken);

        Warn(direct, "compra directa");
    }

    /// <summary>
    /// Mínimo y máximo para todos los libros de demostración; los que no se
    /// compraron quedan agotados y generan alertas.
    /// </summary>
    private async Task SeedStockLevelsAsync(List<DemoBook> books, CancellationToken cancellationToken)
    {
        InventoryCatalogsResponse catalogs = await _inventory.GetCatalogsAsync(cancellationToken);
        int warehouse = catalogs.Warehouses.First().Id;
        string[] shelves = ["A", "B", "C", "D"];

        for (int index = 0; index < books.Count; index++)
        {
            // Algunos mínimos altos para que también haya alertas de "bajo el mínimo".
            int minimum = index % 4 == 0 ? 25 : 5;
            int maximum = index % 4 == 0 ? 60 : 30;

            OperationResult<StockItemResponse> result = await _inventory.SaveLevelsAsync(new SaveStockLevelsRequest
            {
                WarehouseId = warehouse,
                BookId = books[index].Id,
                Minimum = minimum,
                Maximum = maximum,
                Location = $"Estante {shelves[index % shelves.Length]}-{index / shelves.Length + 1}"
            }, includeCosts: false, cancellationToken);

            Warn(result, "niveles de existencia");
        }
    }

    private async Task SeedAdjustmentsAsync(List<DemoBook> books, int userId, CancellationToken cancellationToken)
    {
        InventoryCatalogsResponse catalogs = await _inventory.GetCatalogsAsync(cancellationToken);
        int warehouse = catalogs.Warehouses.First().Id;

        OperationResult<AdjustmentResponse> damage = await _inventory.CreateAdjustmentAsync(new SaveAdjustmentRequest
        {
            ReasonId = catalogs.AdjustmentReasons.Single(reason => reason.Code == "DANIO").Id,
            WarehouseId = warehouse,
            Notes = "Dos ejemplares con humedad detectados en la revisión semanal.",
            Lines = [new AdjustmentLineRequest { BookId = books[0].Id, Quantity = 2 }]
        }, userId, includeCosts: false, cancellationToken);

        Warn(damage, "ajuste por daño");

        OperationResult<AdjustmentResponse> surplus = await _inventory.CreateAdjustmentAsync(new SaveAdjustmentRequest
        {
            ReasonId = catalogs.AdjustmentReasons.Single(reason => reason.Code == "SOBRANTE").Id,
            WarehouseId = warehouse,
            Notes = "Conteo físico mensual: un ejemplar adicional en estante.",
            Lines = [new AdjustmentLineRequest { BookId = books[1].Id, Quantity = 1 }]
        }, userId, includeCosts: false, cancellationToken);

        Warn(surplus, "ajuste por sobrante");
    }

    private async Task<PurchaseOrderResponse?> CreateAsync(SavePurchaseOrderRequest request, int userId, CancellationToken cancellationToken)
    {
        OperationResult<PurchaseOrderResponse> created = await _orders.CreateAsync(request, userId, cancellationToken);
        Warn(created, "orden de compra");
        return created.Data;
    }

    private async Task<PurchaseOrderResponse?> IssueAsync(SavePurchaseOrderRequest request, int userId, CancellationToken cancellationToken)
    {
        PurchaseOrderResponse? order = await CreateAsync(request, userId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        OperationResult<PurchaseOrderResponse> issued = await _orders.IssueAsync(order.Id, userId, cancellationToken);
        Warn(issued, "emisión de orden");
        return issued.Data;
    }

    private async Task ReceiveAsync(
        PurchaseOrderResponse order,
        Func<PurchaseOrderLineResponse, (int Accepted, int Rejected, string? Reason)> quantities,
        string series,
        string number,
        int userId,
        CancellationToken cancellationToken)
    {
        OperationResult<ReceptionResponse> reception = await _receptions.RegisterAsync(new RegisterReceptionRequest
        {
            PurchaseOrderId = order.Id,
            SupplierDocumentSeries = series,
            SupplierDocumentNumber = number,
            Lines = order.Lines
                .Select(line =>
                {
                    (int accepted, int rejected, string? reason) = quantities(line);
                    return new ReceptionLineRequest
                    {
                        PurchaseOrderLineId = line.Id,
                        Accepted = accepted,
                        Rejected = rejected,
                        RejectReason = reason
                    };
                })
                .ToList()
        }, userId, cancellationToken);

        Warn(reception, "recepción");
    }

    /// <summary>Costo de compra: 60 % del precio de venta, sin IVA.</summary>
    private static SavePurchaseOrderRequest Order(
        int supplierId,
        IEnumerable<DemoBook> books,
        int quantity,
        decimal discount,
        int? daysToDeliver = null) => new()
    {
        SupplierId = supplierId,
        ExpectedDate = daysToDeliver is int days ? DateOnly.FromDateTime(DateTime.Today).AddDays(days) : null,
        Lines = books
            .Select((book, index) => new PurchaseOrderLineRequest
            {
                BookId = book.Id,
                Quantity = quantity + index * 2,
                UnitCost = decimal.Round(book.Price * 0.6m, 2),
                DiscountPercent = discount
            })
            .ToList()
    };

    private static SaveSupplierRequest Supplier(
        string legalName, string tradeName, string nit, string email, string phone, decimal creditLimit, int creditDays) => new()
    {
        PersonType = SaveSupplierRequest.LegalPerson,
        LegalName = legalName,
        TradeName = tradeName,
        Nit = nit,
        Email = email,
        Phone = phone,
        CreditLimit = creditLimit,
        CreditDays = creditDays
    };

    /// <summary>DPI ficticio válido (departamento y municipio 01) a partir de 8 dígitos.</summary>
    private static string Dpi(string correlative)
    {
        int total = correlative.Select((digit, index) => (digit - '0') * (index + 2)).Sum();
        int check = total % 11;

        // El verificador debe ser un solo dígito: si sale 10 se ajusta el correlativo.
        return check == 10 ? Dpi((int.Parse(correlative) + 1).ToString("D8")) : $"{correlative}{check}0101";
    }

    private void Warn<T>(OperationResult<T> result, string what)
    {
        if (!result.Succeeded)
        {
            _logger.LogWarning("Demostración ({What}) no registrada: {Errors}", what, string.Join(" ", result.Errors));
        }
    }

    private sealed record DemoBook(int Id, decimal Price);
}
