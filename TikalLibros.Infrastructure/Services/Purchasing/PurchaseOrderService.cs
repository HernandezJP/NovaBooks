using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Purchasing;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services.Purchasing;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;

    public PurchaseOrderService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
    }

    public async Task<PagedResponse<PurchaseOrderListItemResponse>> GetPagedAsync(
        PurchaseOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_ORDEN_COMPRA> orders = _context.OrdenesCompra.AsNoTracking().Where(order => order.OCO_Activo);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            string status = query.Status.Trim().ToUpperInvariant();
            orders = orders.Where(order => order.EstadoOrdenCompra.EOC_Codigo == status);
        }

        if (query.SupplierId.HasValue)
        {
            orders = orders.Where(order => order.OCO_ProveedorId == query.SupplierId.Value);
        }

        if (query.From.HasValue)
        {
            DateTime from = query.From.Value.ToDateTime(TimeOnly.MinValue);
            orders = orders.Where(order => order.OCO_FechaEmision >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            orders = orders.Where(order => order.OCO_FechaEmision < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();

            orders = orders.Where(order =>
                order.OCO_Numero.Contains(value) ||
                order.Proveedor.PRO_Codigo.Contains(value) ||
                (order.Proveedor.Persona.PersonaJuridica != null &&
                 order.Proveedor.Persona.PersonaJuridica.PJU_RazonSocial.Contains(value)) ||
                (order.Proveedor.Persona.PersonaNatural != null &&
                 (order.Proveedor.Persona.PersonaNatural.PNA_PrimerNombre + " " +
                  order.Proveedor.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value)) ||
                order.Detalles.Any(line => line.Libro.LIB_Titulo.Contains(value)));
        }

        int totalItems = await orders.CountAsync(cancellationToken);

        List<PurchaseOrderListItemResponse> items = await orders
            .OrderByDescending(order => order.OCO_FechaCreacion)
            .ThenByDescending(order => order.OCO_OrdenCompra)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new PurchaseOrderListItemResponse
            {
                Id = order.OCO_OrdenCompra,
                Number = order.OCO_Numero,
                SupplierId = order.OCO_ProveedorId,
                SupplierCode = order.Proveedor.PRO_Codigo,
                SupplierName = order.Proveedor.Persona.PersonaJuridica != null
                    ? order.Proveedor.Persona.PersonaJuridica.PJU_RazonSocial
                    : order.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      order.Proveedor.Persona.PersonaNatural.PNA_PrimerApellido,
                Branch = order.Sucursal.SUC_Nombre,
                StatusCode = order.EstadoOrdenCompra.EOC_Codigo,
                Status = order.EstadoOrdenCompra.EOC_Nombre,
                IssueDate = order.OCO_FechaEmision,
                ExpectedDate = order.OCO_FechaEntregaEsperada,
                CurrencySymbol = order.Moneda.MON_Simbolo,
                Total = order.OCO_Total,
                Lines = order.Detalles.Count,
                OrderedUnits = order.Detalles.Sum(line => line.OCD_CantidadSolicitada),
                ReceivedUnits = order.Detalles.Sum(line => line.OCD_CantidadRecibida)
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<PurchaseOrderListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<PurchaseOrderResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        PurchaseOrderResponse? response = await _context.OrdenesCompra
            .AsNoTracking()
            .Where(order => order.OCO_OrdenCompra == id && order.OCO_Activo)
            .Select(order => new PurchaseOrderResponse
            {
                Id = order.OCO_OrdenCompra,
                Number = order.OCO_Numero,
                SupplierId = order.OCO_ProveedorId,
                SupplierCode = order.Proveedor.PRO_Codigo,
                SupplierName = order.Proveedor.Persona.PersonaJuridica != null
                    ? order.Proveedor.Persona.PersonaJuridica.PJU_RazonSocial
                    : order.Proveedor.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      order.Proveedor.Persona.PersonaNatural.PNA_PrimerApellido,
                SupplierNit = order.Proveedor.Persona.Identificaciones
                    .Where(item => item.PID_Activo && item.TipoIdentificacion.TID_Codigo == "NIT")
                    .Select(item => item.PID_Numero)
                    .FirstOrDefault(),
                BranchId = order.OCO_SucursalId,
                Branch = order.Sucursal.SUC_Nombre,
                CurrencyId = order.OCO_MonedaId,
                CurrencyCode = order.Moneda.MON_Codigo,
                CurrencySymbol = order.Moneda.MON_Simbolo,
                ExchangeRate = order.OCO_TipoCambio,
                StatusCode = order.EstadoOrdenCompra.EOC_Codigo,
                Status = order.EstadoOrdenCompra.EOC_Nombre,
                IssueDate = order.OCO_FechaEmision,
                ExpectedDate = order.OCO_FechaEntregaEsperada,
                RequestedBy = order.EmpleadoSolicitante != null
                    ? order.EmpleadoSolicitante.EMP_Codigo + " · " +
                      order.EmpleadoSolicitante.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      order.EmpleadoSolicitante.Persona.PersonaNatural.PNA_PrimerApellido
                    : null,
                Notes = order.OCO_Observaciones,
                Subtotal = order.OCO_Subtotal,
                Discount = order.OCO_Descuento,
                Tax = order.OCO_Impuesto,
                Total = order.OCO_Total,
                Lines = order.Detalles
                    .OrderBy(line => line.OCD_OrdenCompraDetalle)
                    .Select(line => new PurchaseOrderLineResponse
                    {
                        Id = line.OCD_OrdenCompraDetalle,
                        BookId = line.OCD_LibroId,
                        BookCode = line.Libro.LIB_Codigo,
                        Title = line.Libro.LIB_Titulo,
                        Isbn = line.Libro.LIB_ISBN13 ?? line.Libro.LIB_ISBN10,
                        Quantity = line.OCD_CantidadSolicitada,
                        Received = line.OCD_CantidadRecibida,
                        Pending = line.OCD_CantidadSolicitada - line.OCD_CantidadRecibida,
                        UnitCost = line.OCD_CostoUnitario,
                        DiscountPercent = line.OCD_PorcentajeDescuento,
                        DiscountAmount = line.OCD_MontoDescuento,
                        TaxPercent = line.OCD_PorcentajeImpuesto,
                        TaxAmount = line.OCD_MontoImpuesto,
                        Subtotal = line.OCD_Subtotal,
                        Total = line.OCD_Total,
                        Notes = line.OCD_Observaciones
                    })
                    .ToList(),
                CreatedAt = order.OCO_FechaCreacion,
                ModifiedAt = order.OCO_FechaModificacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
        {
            bool anyReceived = response.Lines.Any(line => line.Received > 0);

            response.CanEdit = response.StatusCode == PurchaseOrderStatus.Draft;
            response.CanIssue = response.StatusCode == PurchaseOrderStatus.Draft;
            response.CanCancel = !anyReceived &&
                response.StatusCode is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Issued;
            response.CanReceive = response.StatusCode is PurchaseOrderStatus.Issued or PurchaseOrderStatus.Partial;
        }

        return response;
    }

    public async Task<PurchaseOrderCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        return new PurchaseOrderCatalogsResponse
        {
            Suppliers = await _context.Proveedores
                .AsNoTracking()
                .Where(supplier => supplier.PRO_Activo)
                .Select(supplier => new CatalogOptionResponse
                {
                    Id = supplier.PRO_Proveedor,
                    Code = supplier.PRO_Codigo,
                    Name = supplier.Persona.PersonaJuridica != null
                        ? supplier.Persona.PersonaJuridica.PJU_RazonSocial
                        : supplier.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                          supplier.Persona.PersonaNatural.PNA_PrimerApellido
                })
                .OrderBy(supplier => supplier.Name)
                .ToListAsync(cancellationToken),
            Branches = await _context.Sucursales
                .AsNoTracking()
                .Where(branch => branch.SUC_Activo)
                .OrderByDescending(branch => branch.SUC_EsPrincipal)
                .ThenBy(branch => branch.SUC_Nombre)
                .Select(branch => new CatalogOptionResponse { Id = branch.SUC_Sucursal, Code = branch.SUC_Codigo, Name = branch.SUC_Nombre })
                .ToListAsync(cancellationToken),
            Currencies = await _context.Monedas
                .AsNoTracking()
                .Where(currency => currency.MON_Activo)
                .OrderByDescending(currency => currency.MON_EsPredeterminada)
                .Select(currency => new CurrencyOptionResponse
                {
                    Id = currency.MON_Moneda,
                    Code = currency.MON_Codigo,
                    Name = currency.MON_Nombre,
                    Symbol = currency.MON_Simbolo,
                    IsDefault = currency.MON_EsPredeterminada
                })
                .ToListAsync(cancellationToken),
            Statuses = await _context.EstadosOrdenCompra
                .AsNoTracking()
                .Where(status => status.EOC_Activo)
                .OrderBy(status => status.EOC_EstadoOrdenCompra)
                .Select(status => new CatalogOptionResponse { Id = status.EOC_EstadoOrdenCompra, Code = status.EOC_Codigo, Name = status.EOC_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<IReadOnlyCollection<PurchaseBookOptionResponse>> SearchBooksAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var books = _context.Libros.AsNoTracking().Where(book => book.LIB_Activo);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();
            string digits = new(value.Where(char.IsAsciiDigit).ToArray());

            books = books.Where(book =>
                book.LIB_Titulo.Contains(value) ||
                book.LIB_Codigo.Contains(value) ||
                (digits.Length >= 4 &&
                 ((book.LIB_ISBN13 != null && book.LIB_ISBN13.Contains(digits)) ||
                  (book.LIB_ISBN10 != null && book.LIB_ISBN10.Contains(digits)))));
        }

        string cancelled = PurchaseOrderStatus.Cancelled;

        return await books
            .OrderBy(book => book.LIB_Titulo)
            .Take(20)
            .Select(book => new PurchaseBookOptionResponse
            {
                Id = book.LIB_Libro,
                Code = book.LIB_Codigo,
                Title = book.LIB_Titulo,
                Isbn = book.LIB_ISBN13 ?? book.LIB_ISBN10,
                TaxPercent = book.Impuesto.IMP_Porcentaje,
                LastCost = _context.OrdenesCompraDetalles
                    .Where(line => line.OCD_LibroId == book.LIB_Libro &&
                                   line.OrdenCompra.OCO_Activo &&
                                   line.OrdenCompra.EstadoOrdenCompra.EOC_Codigo != cancelled)
                    .OrderByDescending(line => line.OrdenCompra.OCO_FechaEmision)
                    .Select(line => (decimal?)line.OCD_CostoUnitario)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<PurchaseOrderResponse>> CreateAsync(
        SavePurchaseOrderRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        OperationResult<Resolved> resolved = await ResolveAsync(request, cancellationToken);

        if (!resolved.Succeeded)
        {
            return Fail(resolved);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_ORDEN_COMPRA order = await AddOrderAsync(request, resolved.Data!, employee, PurchaseOrderStatus.Draft, token);

                    return OperationResult<PurchaseOrderResponse>.Success((await GetByIdAsync(order.OCO_OrdenCompra, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(
                "Otra orden se registró al mismo tiempo. Intente guardar nuevamente.");
        }
    }

    public async Task<OperationResult<PurchaseOrderResponse>> UpdateAsync(
        int id,
        SavePurchaseOrderRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        PB_ORDEN_COMPRA? order = await _context.OrdenesCompra
            .Include(item => item.EstadoOrdenCompra)
            .Include(item => item.Detalles)
            .FirstOrDefaultAsync(item => item.OCO_OrdenCompra == id && item.OCO_Activo, cancellationToken);

        if (order is null)
        {
            return OperationResult<PurchaseOrderResponse>.Missing("La orden de compra no existe.");
        }

        if (order.EstadoOrdenCompra.EOC_Codigo != PurchaseOrderStatus.Draft)
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(
                $"La orden {order.OCO_Numero} está {order.EstadoOrdenCompra.EOC_Nombre.ToLowerInvariant()} y ya no puede modificarse.");
        }

        if (await _employees.FindAsync(authenticatedUserId, cancellationToken) is null)
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        OperationResult<Resolved> resolved = await ResolveAsync(request, cancellationToken);

        if (!resolved.Succeeded)
        {
            return Fail(resolved);
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                order.OCO_FechaModificacion = DateTime.UtcNow;
                Apply(order, request, resolved.Data!);

                await _context.SaveChangesAsync(token);

                return OperationResult<PurchaseOrderResponse>.Success((await GetByIdAsync(order.OCO_OrdenCompra, token))!);
            },
            cancellationToken);
    }

    public async Task<OperationResult<PurchaseOrderResponse>> IssueAsync(
        int id,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        PB_ORDEN_COMPRA? order = await _context.OrdenesCompra
            .Include(item => item.EstadoOrdenCompra)
            .Include(item => item.Proveedor)
            .Include(item => item.Detalles)
            .FirstOrDefaultAsync(item => item.OCO_OrdenCompra == id && item.OCO_Activo, cancellationToken);

        if (order is null)
        {
            return OperationResult<PurchaseOrderResponse>.Missing("La orden de compra no existe.");
        }

        if (order.EstadoOrdenCompra.EOC_Codigo != PurchaseOrderStatus.Draft)
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(
                $"Solo se emiten órdenes en borrador; la orden {order.OCO_Numero} está {order.EstadoOrdenCompra.EOC_Nombre.ToLowerInvariant()}.");
        }

        if (await _employees.FindAsync(authenticatedUserId, cancellationToken) is null)
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        List<string> problems = [];

        if (!order.Proveedor.PRO_Activo)
        {
            problems.Add($"El proveedor {order.Proveedor.PRO_Codigo} está inactivo.");
        }

        if (order.Detalles.Count == 0)
        {
            problems.Add("La orden no tiene libros.");
        }

        if (order.OCO_FechaEntregaEsperada is { } expected && expected < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            problems.Add("La fecha de entrega esperada ya pasó; actualícela antes de emitir.");
        }

        int[] bookIds = order.Detalles.Select(line => line.OCD_LibroId).ToArray();
        List<string> inactiveBooks = await _context.Libros
            .Where(book => bookIds.Contains(book.LIB_Libro) && !book.LIB_Activo)
            .Select(book => book.LIB_Titulo)
            .ToListAsync(cancellationToken);

        if (inactiveBooks.Count > 0)
        {
            problems.Add($"Hay libros inactivos en la orden: {string.Join(", ", inactiveBooks)}.");
        }

        if (problems.Count > 0)
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(problems.ToArray());
        }

        order.OCO_EstadoOrdenCompraId = await StatusIdAsync(PurchaseOrderStatus.Issued, cancellationToken);
        order.OCO_FechaEmision = DateTime.UtcNow;
        order.OCO_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<PurchaseOrderResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<OperationResult<PurchaseOrderResponse>> CancelAsync(
        int id,
        CancelPurchaseOrderRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        PB_ORDEN_COMPRA? order = await _context.OrdenesCompra
            .Include(item => item.EstadoOrdenCompra)
            .Include(item => item.Detalles)
            .FirstOrDefaultAsync(item => item.OCO_OrdenCompra == id && item.OCO_Activo, cancellationToken);

        if (order is null)
        {
            return OperationResult<PurchaseOrderResponse>.Missing("La orden de compra no existe.");
        }

        if (order.EstadoOrdenCompra.EOC_Codigo is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Issued) ||
            order.Detalles.Any(line => line.OCD_CantidadRecibida > 0))
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(
                $"La orden {order.OCO_Numero} no puede cancelarse porque está " +
                $"{order.EstadoOrdenCompra.EOC_Nombre.ToLowerInvariant()} o ya tiene libros recibidos.");
        }

        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<PurchaseOrderResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        string note = $"[Cancelada el {DateTime.UtcNow:dd/MM/yyyy} por {employee.Code}] {request.Reason.Trim()}";

        order.OCO_EstadoOrdenCompraId = await StatusIdAsync(PurchaseOrderStatus.Cancelled, cancellationToken);
        order.OCO_Observaciones = string.IsNullOrWhiteSpace(order.OCO_Observaciones)
            ? note
            : $"{order.OCO_Observaciones}\n{note}";
        order.OCO_Observaciones = order.OCO_Observaciones.Length > 1000
            ? order.OCO_Observaciones[^1000..]
            : order.OCO_Observaciones;
        order.OCO_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<PurchaseOrderResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    /// <summary>
    /// Crea una orden ya emitida para una compra directa. Debe llamarse
    /// dentro de la transacciÃ³n de quien registra la recepciÃ³n.
    /// </summary>
    internal async Task<OperationResult<PB_ORDEN_COMPRA>> CreateIssuedAsync(
        SavePurchaseOrderRequest request,
        CurrentEmployee employee,
        CancellationToken cancellationToken)
    {
        OperationResult<Resolved> resolved = await ResolveAsync(request, cancellationToken);

        if (!resolved.Succeeded)
        {
            return resolved.ErrorType == OperationErrorType.Conflict
                ? OperationResult<PB_ORDEN_COMPRA>.Conflict(resolved.Errors.ToArray())
                : OperationResult<PB_ORDEN_COMPRA>.Failure(resolved.Errors.ToArray());
        }

        return OperationResult<PB_ORDEN_COMPRA>.Success(
            await AddOrderAsync(request, resolved.Data!, employee, PurchaseOrderStatus.Issued, cancellationToken));
    }

    private async Task<PB_ORDEN_COMPRA> AddOrderAsync(
        SavePurchaseOrderRequest request,
        Resolved resolved,
        CurrentEmployee employee,
        string status,
        CancellationToken cancellationToken)
    {
        PB_ORDEN_COMPRA order = new()
        {
            OCO_Numero = await _numbers.NextAsync(DocumentTypes.PurchaseOrder, cancellationToken),
            OCO_EstadoOrdenCompraId = await StatusIdAsync(status, cancellationToken),
            OCO_EmpleadoSolicitanteId = employee.Id,
            OCO_FechaEmision = DateTime.UtcNow,
            OCO_Activo = true,
            OCO_FechaCreacion = DateTime.UtcNow
        };

        Apply(order, request, resolved);
        _context.OrdenesCompra.Add(order);

        await _context.SaveChangesAsync(cancellationToken);

        return order;
    }

    /// <summary>
    /// Valida proveedor, sucursal, moneda y libros, y devuelve los datos

    /// necesarios para calcular la orden.
    /// </summary>
    private async Task<OperationResult<Resolved>> ResolveAsync(
        SavePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        List<string> errors = [];

        var supplier = await _context.Proveedores
            .AsNoTracking()
            .Where(item => item.PRO_Proveedor == request.SupplierId)
            .Select(item => new { item.PRO_Codigo, item.PRO_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier is null)
        {
            errors.Add("El proveedor seleccionado no existe.");
        }
        else if (!supplier.PRO_Activo)
        {
            errors.Add($"El proveedor {supplier.PRO_Codigo} está inactivo; no se le pueden hacer compras.");
        }

        int? branchId = request.BranchId.HasValue
            ? await _context.Sucursales
                .Where(item => item.SUC_Sucursal == request.BranchId && item.SUC_Activo)
                .Select(item => (int?)item.SUC_Sucursal)
                .FirstOrDefaultAsync(cancellationToken)
            : await _context.Sucursales
                .Where(item => item.SUC_EsPrincipal && item.SUC_Activo)
                .Select(item => (int?)item.SUC_Sucursal)
                .FirstOrDefaultAsync(cancellationToken);

        if (branchId is null)
        {
            errors.Add("La sucursal seleccionada no existe o está inactiva.");
        }

        var currency = await _context.Monedas
            .AsNoTracking()
            .Where(item => item.MON_Activo &&
                           (request.CurrencyId.HasValue ? item.MON_Moneda == request.CurrencyId : item.MON_EsPredeterminada))
            .Select(item => new { item.MON_Moneda, item.MON_EsPredeterminada, item.MON_Codigo })
            .FirstOrDefaultAsync(cancellationToken);

        decimal exchangeRate = 1m;

        if (currency is null)
        {
            errors.Add("La moneda seleccionada no existe o está inactiva.");
        }
        else if (!currency.MON_EsPredeterminada)
        {
            if (request.ExchangeRate is not > 0)
            {
                errors.Add($"Indique el tipo de cambio para compras en {currency.MON_Codigo}.");
            }
            else
            {
                exchangeRate = request.ExchangeRate.Value;
            }
        }

        int[] bookIds = request.Lines.Select(line => line.BookId!.Value).Distinct().ToArray();

        Dictionary<int, BookData> books = (await _context.Libros
                .AsNoTracking()
                .Where(book => bookIds.Contains(book.LIB_Libro))
                .Select(book => new { book.LIB_Libro, book.LIB_Titulo, book.LIB_Activo, book.Impuesto.IMP_Porcentaje })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                book => book.LIB_Libro,
                book => new BookData(book.LIB_Titulo, book.LIB_Activo, book.IMP_Porcentaje));


        string[] missing = bookIds.Where(id => !books.ContainsKey(id)).Select(id => id.ToString()).ToArray();

        if (missing.Length > 0)
        {
            errors.Add($"Los libros con id {string.Join(", ", missing)} no existen.");
        }

        string[] inactive = books.Values.Where(book => !book.IsActive).Select(book => book.Title).ToArray();

        if (inactive.Length > 0)
        {
            errors.Add($"No se pueden comprar libros inactivos: {string.Join(", ", inactive)}.");
        }

        return errors.Count > 0
            ? OperationResult<Resolved>.Failure(errors.ToArray())
            : OperationResult<Resolved>.Success(new Resolved(branchId!.Value, currency!.MON_Moneda, exchangeRate, books));
    }

    /// <summary>
    /// Copia la cabecera y sincroniza las líneas por libro (se actualizan,
    /// agregan o quitan) y recalcula los totales.
    /// </summary>
    private void Apply(PB_ORDEN_COMPRA order, SavePurchaseOrderRequest request, Resolved resolved)
    {
        order.OCO_ProveedorId = request.SupplierId!.Value;
        order.OCO_SucursalId = resolved.BranchId;
        order.OCO_MonedaId = resolved.CurrencyId;
        order.OCO_TipoCambio = resolved.ExchangeRate;
        order.OCO_FechaEntregaEsperada = request.ExpectedDate;
        order.OCO_Observaciones = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        HashSet<int> requestedBooks = request.Lines.Select(line => line.BookId!.Value).ToHashSet();

        foreach (PB_ORDEN_COMPRA_DETALLE removed in order.Detalles.Where(line => !requestedBooks.Contains(line.OCD_LibroId)).ToList())
        {
            order.Detalles.Remove(removed);
            _context.OrdenesCompraDetalles.Remove(removed);
        }

        foreach (PurchaseOrderLineRequest line in request.Lines)
        {
            int bookId = line.BookId!.Value;
            PB_ORDEN_COMPRA_DETALLE? detail = order.Detalles.FirstOrDefault(item => item.OCD_LibroId == bookId);

            if (detail is null)
            {
                detail = new PB_ORDEN_COMPRA_DETALLE { OCD_LibroId = bookId };
                order.Detalles.Add(detail);
            }

            decimal taxPercent = resolved.Books[bookId].TaxPercent;
            LineAmounts amounts = LineAmounts.Calculate(line.Quantity!.Value, line.UnitCost!.Value, line.DiscountPercent, taxPercent);

            detail.OCD_CantidadSolicitada = line.Quantity.Value;
            detail.OCD_CostoUnitario = line.UnitCost.Value;
            detail.OCD_PorcentajeDescuento = line.DiscountPercent;
            detail.OCD_MontoDescuento = amounts.Discount;
            detail.OCD_PorcentajeImpuesto = taxPercent;
            detail.OCD_MontoImpuesto = amounts.Tax;
            detail.OCD_Subtotal = amounts.Subtotal;
            detail.OCD_Total = amounts.Total;
            detail.OCD_Observaciones = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim();
        }

        order.OCO_Subtotal = order.Detalles.Sum(line => line.OCD_Subtotal);
        order.OCO_Descuento = order.Detalles.Sum(line => line.OCD_MontoDescuento);
        order.OCO_Impuesto = order.Detalles.Sum(line => line.OCD_MontoImpuesto);
        order.OCO_Total = order.Detalles.Sum(line => line.OCD_Total);
    }

    private Task<int> StatusIdAsync(string code, CancellationToken cancellationToken) =>
        _context.EstadosOrdenCompra
            .Where(status => status.EOC_Codigo == code)
            .Select(status => status.EOC_EstadoOrdenCompra)
            .SingleAsync(cancellationToken);

    private static OperationResult<PurchaseOrderResponse> Fail<T>(OperationResult<T> result) =>
        result.ErrorType == OperationErrorType.Conflict
            ? OperationResult<PurchaseOrderResponse>.Conflict(result.Errors.ToArray())
            : OperationResult<PurchaseOrderResponse>.Failure(result.Errors.ToArray());

    private sealed record BookData(string Title, bool IsActive, decimal TaxPercent);

    private sealed record Resolved(int BranchId, int CurrencyId, decimal ExchangeRate, Dictionary<int, BookData> Books);
}
