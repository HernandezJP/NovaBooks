using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services.Inventory;

/// <summary>
/// Traslados entre almacenes. La salida del origen sigue PEPS (los lotes más
/// antiguos primero) y en el destino cada lote conserva su número, costo y
/// fecha de ingreso, de modo que la antigüedad y la valorización no cambian.
/// Solo se trasladan unidades libres (no reservadas por pedidos).
/// </summary>
public sealed class TransferService : ITransferService
{
    public const string TransferType = "TRANSFERENCIA";

    private readonly AppDbContext _context;
    private readonly InventoryService _inventory;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;

    public TransferService(
        AppDbContext context,
        InventoryService inventory,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees)
    {
        _context = context;
        _inventory = inventory;
        _numbers = numbers;
        _employees = employees;
    }

    public async Task<PagedResponse<TransferListItemResponse>> GetPagedAsync(
        TransferQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_MOVIMIENTO_INVENTARIO> transfers = _context.MovimientosInventario
            .AsNoTracking()
            .Where(item => item.TipoMovimiento.TMI_Codigo == TransferType);

        if (!query.IncludeAnnulled)
        {
            transfers = transfers.Where(item => !item.MOV_Anulado);
        }

        if (query.WarehouseId.HasValue)
        {
            transfers = transfers.Where(item =>
                item.MOV_AlmacenOrigenId == query.WarehouseId || item.MOV_AlmacenDestinoId == query.WarehouseId);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            transfers = transfers.Where(item => item.MOV_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            transfers = transfers.Where(item => item.MOV_Fecha < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            transfers = transfers.Where(item =>
                item.MOV_Numero.Contains(value) ||
                item.Detalles.Any(line => line.Libro.LIB_Titulo.Contains(value) || line.Libro.LIB_Codigo.Contains(value)));
        }

        int totalItems = await transfers.CountAsync(cancellationToken);

        List<TransferListItemResponse> items = await transfers
            .OrderByDescending(item => item.MOV_Fecha)
            .ThenByDescending(item => item.MOV_MovimientoInventario)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new TransferListItemResponse
            {
                Id = item.MOV_MovimientoInventario,
                Number = item.MOV_Numero,
                Date = item.MOV_Fecha,
                SourceWarehouse = item.AlmacenOrigen!.ALM_Nombre,
                SourceBranch = item.AlmacenOrigen.Sucursal.SUC_Nombre,
                TargetWarehouse = item.AlmacenDestino!.ALM_Nombre,
                TargetBranch = item.AlmacenDestino.Sucursal.SUC_Nombre,
                Employee = item.Empleado != null
                    ? item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido
                    : null,
                Books = item.Detalles.Select(line => line.MDE_LibroId).Distinct().Count(),
                Units = item.Detalles.Sum(line => line.MDE_Cantidad),
                Notes = item.MOV_Observaciones,
                Annulled = item.MOV_Anulado
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<TransferListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<OperationResult<MovementResponse>> CreateAsync(
        SaveTransferRequest request,
        int authenticatedUserId,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<MovementResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        var warehouses = await _context.Almacenes
            .AsNoTracking()
            .Where(item => (item.ALM_Almacen == request.SourceWarehouseId || item.ALM_Almacen == request.TargetWarehouseId) && item.ALM_Activo)
            .Select(item => new { item.ALM_Almacen, item.ALM_Nombre })
            .ToDictionaryAsync(item => item.ALM_Almacen, item => item.ALM_Nombre, cancellationToken);

        List<string> errors = [];

        if (!warehouses.ContainsKey(request.SourceWarehouseId!.Value))
        {
            errors.Add("El almacén de origen no existe o está inactivo.");
        }

        if (!warehouses.ContainsKey(request.TargetWarehouseId!.Value))
        {
            errors.Add("El almacén de destino no existe o está inactivo.");
        }

        int[] bookIds = request.Lines.Select(line => line.BookId!.Value).ToArray();

        Dictionary<int, (string Title, decimal ReferenceCost)> books = await _context.Libros
            .AsNoTracking()
            .Where(book => bookIds.Contains(book.LIB_Libro))
            .ToDictionaryAsync(book => book.LIB_Libro, book => (book.LIB_Titulo, book.LIB_CostoReferencia), cancellationToken);

        string[] missing = bookIds.Where(id => !books.ContainsKey(id)).Select(id => id.ToString()).ToArray();

        if (missing.Length > 0)
        {
            errors.Add($"Los libros con id {string.Join(", ", missing)} no existen.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<MovementResponse>.Failure(errors.ToArray());
        }

        int source = request.SourceWarehouseId.Value;
        int target = request.TargetWarehouseId.Value;

        try
        {
            return await _context.ExecuteInTransactionAsync(async token =>
            {
                DateTime now = DateTime.UtcNow;
                List<string> shortages = [];

                PB_MOVIMIENTO_INVENTARIO movement = new()
                {
                    MOV_Numero = await _numbers.NextAsync(DocumentTypes.InventoryMovement, token),
                    MOV_TipoMovimientoId = await _context.TiposMovimientoInventario
                        .Where(type => type.TMI_Codigo == TransferType)
                        .Select(type => type.TMI_TipoMovimientoInventario)
                        .SingleAsync(token),
                    MOV_AlmacenOrigenId = source,
                    MOV_AlmacenDestinoId = target,
                    MOV_EmpleadoId = employee.Id,
                    MOV_Fecha = now,
                    MOV_DocumentoReferencia = Truncate($"Traslado {warehouses[source]} → {warehouses[target]}", 100),
                    MOV_Observaciones = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    MOV_Procesado = true,
                    MOV_FechaProcesado = now,
                    MOV_FechaCreacion = now
                };

                foreach (TransferLineRequest line in request.Lines)
                {
                    int bookId = line.BookId!.Value;
                    decimal quantity = line.Quantity!.Value;

                    PB_EXISTENCIA? origin = await _context.Existencias.FirstOrDefaultAsync(
                        item => item.EXI_AlmacenId == source && item.EXI_LibroId == bookId, token);

                    decimal free = (origin?.EXI_CantidadDisponible ?? 0) - (origin?.EXI_CantidadReservada ?? 0);

                    if (origin is null || quantity > free)
                    {
                        shortages.Add($"\"{books[bookId].Title}\": se quieren trasladar {quantity:0} y hay {Math.Max(free, 0):0} libres en {warehouses[source]}.");
                        continue;
                    }

                    int firstLine = movement.Detalles.Count;
                    await _inventory.ConsumeLotsAsync(movement, source, bookId, quantity, books[bookId].ReferenceCost, token);

                    // Cada lote retirado del origen entra al destino con su número, costo y fecha.
                    foreach (PB_MOVIMIENTO_INVENTARIO_DETALLE detail in movement.Detalles.Skip(firstLine).ToList())
                    {
                        await ReceiveLotAsync(movement, target, detail, now, token);
                    }

                    PB_EXISTENCIA destination = await StockAsync(target, bookId, now, token);

                    origin.EXI_CantidadDisponible -= quantity;
                    origin.EXI_FechaUltimoMovimiento = now;
                    origin.EXI_FechaModificacion = now;
                    destination.EXI_CantidadDisponible += quantity;
                    destination.EXI_FechaUltimoMovimiento = now;
                    destination.EXI_FechaModificacion = now;
                }

                if (shortages.Count > 0)
                {
                    return OperationResult<MovementResponse>.Conflict(shortages.ToArray());
                }

                _context.MovimientosInventario.Add(movement);
                await _context.SaveChangesAsync(token);

                return OperationResult<MovementResponse>.Success(
                    (await _inventory.GetMovementAsync(movement.MOV_MovimientoInventario, includeCosts, token))!);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<MovementResponse>.Conflict(
                "El inventario cambió mientras se guardaba el traslado. Intente nuevamente.");
        }
    }

    /// <summary>
    /// Devuelve las unidades al almacén de origen. Solo es posible si siguen
    /// libres en el destino (no se vendieron, reservaron ni movieron).
    /// </summary>
    public async Task<OperationResult<MovementResponse>> AnnulAsync(
        int id,
        AnnulTransferRequest request,
        int authenticatedUserId,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        if (await _employees.FindAsync(authenticatedUserId, cancellationToken) is null)
        {
            return OperationResult<MovementResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(async token =>
            {
                PB_MOVIMIENTO_INVENTARIO? movement = await _context.MovimientosInventario
                    .Include(item => item.Detalles).ThenInclude(line => line.Lote)
                    .Include(item => item.Detalles).ThenInclude(line => line.Libro)
                    .Include(item => item.AlmacenDestino)
                    .FirstOrDefaultAsync(item => item.MOV_MovimientoInventario == id && item.TipoMovimiento.TMI_Codigo == TransferType, token);

                if (movement is null)
                {
                    return OperationResult<MovementResponse>.Missing("El traslado solicitado no existe.");
                }

                if (movement.MOV_Anulado)
                {
                    return OperationResult<MovementResponse>.Conflict($"El traslado {movement.MOV_Numero} ya está anulado.");
                }

                int source = movement.MOV_AlmacenOrigenId!.Value;
                int target = movement.MOV_AlmacenDestinoId!.Value;
                DateTime now = DateTime.UtcNow;
                List<string> blocked = [];

                foreach (var book in movement.Detalles.GroupBy(line => line.MDE_LibroId))
                {
                    decimal quantity = book.Sum(line => line.MDE_Cantidad);
                    PB_EXISTENCIA? destination = await _context.Existencias.FirstOrDefaultAsync(
                        item => item.EXI_AlmacenId == target && item.EXI_LibroId == book.Key, token);
                    decimal free = (destination?.EXI_CantidadDisponible ?? 0) - (destination?.EXI_CantidadReservada ?? 0);

                    if (destination is null || free < quantity)
                    {
                        blocked.Add($"\"{book.First().Libro.LIB_Titulo}\": se trasladaron {quantity:0} y solo quedan {Math.Max(free, 0):0} libres en {movement.AlmacenDestino!.ALM_Nombre}.");
                        continue;
                    }

                    foreach (PB_MOVIMIENTO_INVENTARIO_DETALLE line in book)
                    {
                        if (!await TakeBackLotAsync(movement, target, line, now, token))
                        {
                            blocked.Add($"\"{line.Libro.LIB_Titulo}\": el lote {line.Lote?.LOT_Numero ?? movement.MOV_Numero} ya no tiene las unidades trasladadas en {movement.AlmacenDestino!.ALM_Nombre}.");
                        }

                        if (line.Lote is not null)
                        {
                            line.Lote.LOT_CantidadActual += line.MDE_Cantidad;
                            line.Lote.LOT_Activo = true;
                            line.Lote.LOT_FechaModificacion = now;
                        }
                    }

                    PB_EXISTENCIA origin = await StockAsync(source, book.Key, now, token);

                    destination.EXI_CantidadDisponible -= quantity;
                    destination.EXI_FechaUltimoMovimiento = now;
                    destination.EXI_FechaModificacion = now;
                    origin.EXI_CantidadDisponible += quantity;
                    origin.EXI_FechaUltimoMovimiento = now;
                    origin.EXI_FechaModificacion = now;
                }

                if (blocked.Count > 0)
                {
                    return OperationResult<MovementResponse>.Conflict(blocked.ToArray());
                }

                movement.MOV_Anulado = true;
                movement.MOV_FechaAnulacion = now;
                movement.MOV_MotivoAnulacion = request.Reason.Trim();

                await _context.SaveChangesAsync(token);

                return OperationResult<MovementResponse>.Success((await _inventory.GetMovementAsync(id, includeCosts, token))!);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<MovementResponse>.Conflict(
                "El inventario cambió mientras se anulaba el traslado. Intente nuevamente.");
        }
    }

    /// <summary>
    /// Entrada al destino de una línea retirada del origen: se suma al lote
    /// con el mismo número y costo, o se crea uno nuevo (si el número ya existe
    /// con otro costo, se le agrega el número del traslado).
    /// </summary>
    private async Task ReceiveLotAsync(
        PB_MOVIMIENTO_INVENTARIO movement,
        int target,
        PB_MOVIMIENTO_INVENTARIO_DETALLE detail,
        DateTime now,
        CancellationToken cancellationToken)
    {
        PB_LOTE? origin = detail.Lote;
        string number = origin?.LOT_Numero ?? movement.MOV_Numero;

        PB_LOTE? existing = await FindLotAsync(target, detail.MDE_LibroId, number, cancellationToken);

        if (existing is not null && existing.LOT_CostoUnitario != detail.MDE_CostoUnitario)
        {
            number = TransferLotNumber(number, movement.MOV_Numero);
            existing = await FindLotAsync(target, detail.MDE_LibroId, number, cancellationToken);
        }

        if (existing is not null)
        {
            existing.LOT_CantidadInicial += detail.MDE_Cantidad;
            existing.LOT_CantidadActual += detail.MDE_Cantidad;
            existing.LOT_Activo = true;
            existing.LOT_FechaModificacion = now;
            return;
        }

        _context.Lotes.Add(new PB_LOTE
        {
            LOT_AlmacenId = target,
            LOT_LibroId = detail.MDE_LibroId,
            LOT_ProveedorId = origin?.LOT_ProveedorId,
            LOT_Numero = number,
            LOT_FechaIngreso = origin?.LOT_FechaIngreso ?? BusinessTime.Today,
            LOT_FechaVencimiento = origin?.LOT_FechaVencimiento,
            LOT_CantidadInicial = detail.MDE_Cantidad,
            LOT_CantidadActual = detail.MDE_Cantidad,
            LOT_CostoUnitario = detail.MDE_CostoUnitario,
            LOT_Activo = true,
            LOT_FechaCreacion = now
        });
    }

    /// <summary>Retira del destino el lote que creó o incrementó el traslado.</summary>
    private async Task<bool> TakeBackLotAsync(
        PB_MOVIMIENTO_INVENTARIO movement,
        int target,
        PB_MOVIMIENTO_INVENTARIO_DETALLE line,
        DateTime now,
        CancellationToken cancellationToken)
    {
        string number = line.Lote?.LOT_Numero ?? movement.MOV_Numero;

        PB_LOTE? lot = await FindLotAsync(target, line.MDE_LibroId, TransferLotNumber(number, movement.MOV_Numero), cancellationToken)
                       ?? await FindLotAsync(target, line.MDE_LibroId, number, cancellationToken);

        if (lot is null || lot.LOT_CantidadActual < line.MDE_Cantidad)
        {
            return false;
        }

        lot.LOT_CantidadActual -= line.MDE_Cantidad;
        lot.LOT_Activo = lot.LOT_CantidadActual > 0;
        lot.LOT_FechaModificacion = now;
        return true;
    }

    private async Task<PB_LOTE?> FindLotAsync(int warehouse, int book, string number, CancellationToken cancellationToken) =>
        _context.Lotes.Local.FirstOrDefault(lot => lot.LOT_AlmacenId == warehouse && lot.LOT_LibroId == book && lot.LOT_Numero == number)
        ?? await _context.Lotes.FirstOrDefaultAsync(lot => lot.LOT_AlmacenId == warehouse && lot.LOT_LibroId == book && lot.LOT_Numero == number, cancellationToken);

    private async Task<PB_EXISTENCIA> StockAsync(int warehouse, int book, DateTime now, CancellationToken cancellationToken)
    {
        PB_EXISTENCIA? stock =
            _context.Existencias.Local.FirstOrDefault(item => item.EXI_AlmacenId == warehouse && item.EXI_LibroId == book)
            ?? await _context.Existencias.FirstOrDefaultAsync(item => item.EXI_AlmacenId == warehouse && item.EXI_LibroId == book, cancellationToken);

        if (stock is null)
        {
            stock = new PB_EXISTENCIA { EXI_AlmacenId = warehouse, EXI_LibroId = book, EXI_FechaCreacion = now };
            _context.Existencias.Add(stock);
        }

        return stock;
    }

    private static string TransferLotNumber(string lot, string movement) => Truncate($"{lot}/{movement}", 50);

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
