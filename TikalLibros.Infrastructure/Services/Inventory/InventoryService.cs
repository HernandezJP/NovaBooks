using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;
using TikalLibros.Infrastructure.Services.Purchasing;

namespace TikalLibros.Infrastructure.Services.Inventory;

public sealed class InventoryService : IInventoryService
{
    public const string PositiveAdjustmentType = "AJUSTE_POSITIVO";
    public const string NegativeAdjustmentType = "AJUSTE_NEGATIVO";

    private const int KardexLimit = 1000;

    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;

    public InventoryService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
    }

    public async Task<InventoryCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default)
    {
        return new InventoryCatalogsResponse
        {
            Warehouses = await _context.Almacenes
                .AsNoTracking()
                .Where(item => item.ALM_Activo)
                .OrderByDescending(item => item.ALM_EsPrincipal)
                .ThenBy(item => item.ALM_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.ALM_Almacen, Code = item.ALM_Codigo, Name = item.ALM_Nombre })
                .ToListAsync(cancellationToken),
            AdjustmentReasons = await _context.MotivosAjuste
                .AsNoTracking()
                .Where(item => item.MAJ_Activo)
                .OrderBy(item => item.MAJ_Nombre)
                .Select(item => new AdjustmentReasonResponse
                {
                    Id = item.MAJ_MotivoAjuste,
                    Code = item.MAJ_Codigo,
                    Name = item.MAJ_Nombre,
                    Nature = item.MAJ_Naturaleza,
                    RequiresNotes = item.MAJ_RequiereObservacion
                })
                .ToListAsync(cancellationToken),
            MovementTypes = await _context.TiposMovimientoInventario
                .AsNoTracking()
                .Where(item => item.TMI_Activo)
                .OrderBy(item => item.TMI_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.TMI_TipoMovimientoInventario, Code = item.TMI_Codigo, Name = item.TMI_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PagedResponse<StockItemResponse>> GetStockAsync(
        StockQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_EXISTENCIA> stock = _context.Existencias.AsNoTracking();

        if (query.WarehouseId.HasValue)
        {
            stock = stock.Where(item => item.EXI_AlmacenId == query.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            string digits = new(value.Where(char.IsAsciiDigit).ToArray());

            stock = stock.Where(item =>
                item.Libro.LIB_Titulo.Contains(value) ||
                item.Libro.LIB_Codigo.Contains(value) ||
                (item.EXI_Ubicacion != null && item.EXI_Ubicacion.Contains(value)) ||
                (digits.Length >= 4 &&
                 ((item.Libro.LIB_ISBN13 != null && item.Libro.LIB_ISBN13.Contains(digits)) ||
                  (item.Libro.LIB_ISBN10 != null && item.Libro.LIB_ISBN10.Contains(digits)))));
        }

        stock = FilterByStatus(stock, query.Status);

        int totalItems = await stock.CountAsync(cancellationToken);

        var rows = await stock
            .OrderBy(item => item.Libro.LIB_Titulo)
            .ThenBy(item => item.Almacen.ALM_Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.EXI_Existencia,
                item.EXI_LibroId,
                item.Libro.LIB_Codigo,
                item.Libro.LIB_Titulo,
                Isbn = item.Libro.LIB_ISBN13 ?? item.Libro.LIB_ISBN10,
                item.EXI_AlmacenId,
                item.Almacen.ALM_Nombre,
                item.EXI_CantidadDisponible,
                item.EXI_CantidadReservada,
                item.EXI_StockMinimo,
                item.EXI_StockMaximo,
                item.EXI_Ubicacion,
                item.EXI_FechaUltimoMovimiento,
                LotUnits = _context.Lotes
                    .Where(lot => lot.LOT_AlmacenId == item.EXI_AlmacenId && lot.LOT_LibroId == item.EXI_LibroId && lot.LOT_Activo)
                    .Sum(lot => lot.LOT_CantidadActual),
                LotValue = _context.Lotes
                    .Where(lot => lot.LOT_AlmacenId == item.EXI_AlmacenId && lot.LOT_LibroId == item.EXI_LibroId && lot.LOT_Activo)
                    .Sum(lot => lot.LOT_CantidadActual * lot.LOT_CostoUnitario)
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<StockItemResponse>
        {
            Items = rows
                .Select(row =>
                {
                    string status = Classify(row.EXI_CantidadDisponible, row.EXI_StockMinimo, row.EXI_StockMaximo);

                    return new StockItemResponse
                    {
                        Id = row.EXI_Existencia,
                        BookId = row.EXI_LibroId,
                        BookCode = row.LIB_Codigo,
                        Title = row.LIB_Titulo,
                        Isbn = row.Isbn,
                        WarehouseId = row.EXI_AlmacenId,
                        Warehouse = row.ALM_Nombre,
                        Available = row.EXI_CantidadDisponible,
                        Reserved = row.EXI_CantidadReservada,
                        Free = row.EXI_CantidadDisponible - row.EXI_CantidadReservada,
                        Minimum = row.EXI_StockMinimo,
                        Maximum = row.EXI_StockMaximo,
                        Location = row.EXI_Ubicacion,
                        LastMovementAt = row.EXI_FechaUltimoMovimiento,
                        Status = status,
                        StatusLabel = StockStatus.Label(status),
                        AverageCost = includeCosts && row.LotUnits > 0
                            ? decimal.Round(row.LotValue / row.LotUnits, 4, MidpointRounding.AwayFromZero)
                            : null,
                        TotalValue = includeCosts ? LineAmounts.Round(row.LotValue) : null
                    };
                })
                .ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<OperationResult<StockItemResponse>> SaveLevelsAsync(
        SaveStockLevelsRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        List<string> errors = [];

        if (!await _context.Almacenes.AnyAsync(item => item.ALM_Almacen == request.WarehouseId && item.ALM_Activo, cancellationToken))
        {
            errors.Add("El almacén seleccionado no existe o está inactivo.");
        }

        if (!await _context.Libros.AnyAsync(item => item.LIB_Libro == request.BookId, cancellationToken))
        {
            errors.Add("El libro seleccionado no existe.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<StockItemResponse>.Failure(errors.ToArray());
        }

        PB_EXISTENCIA? stock = await _context.Existencias.FirstOrDefaultAsync(
            item => item.EXI_AlmacenId == request.WarehouseId && item.EXI_LibroId == request.BookId,
            cancellationToken);

        DateTime now = DateTime.UtcNow;

        if (stock is null)
        {
            stock = new PB_EXISTENCIA
            {
                EXI_AlmacenId = request.WarehouseId!.Value,
                EXI_LibroId = request.BookId!.Value,
                EXI_FechaCreacion = now
            };

            _context.Existencias.Add(stock);
        }

        stock.EXI_StockMinimo = request.Minimum;
        stock.EXI_StockMaximo = request.Maximum;
        stock.EXI_Ubicacion = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim().ToUpperInvariant();
        stock.EXI_FechaModificacion = now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<StockItemResponse>.Conflict("La existencia cambió mientras se guardaba. Intente nuevamente.");
        }

        PagedResponse<StockItemResponse> saved = await GetStockAsync(
            new StockQuery { WarehouseId = stock.EXI_AlmacenId, Search = await BookCodeAsync(stock.EXI_LibroId, cancellationToken), PageSize = 100 },
            includeCosts,
            cancellationToken);

        return OperationResult<StockItemResponse>.Success(saved.Items.Single(item => item.Id == stock.EXI_Existencia));
    }

    public async Task<IReadOnlyCollection<StockAlertResponse>> GetAlertsAsync(
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PB_EXISTENCIA> stock = AlertsQuery(_context.Existencias.AsNoTracking());

        if (warehouseId.HasValue)
        {
            stock = stock.Where(item => item.EXI_AlmacenId == warehouseId.Value);
        }

        var rows = await stock
            .OrderBy(item => item.EXI_CantidadDisponible)
            .ThenBy(item => item.Libro.LIB_Titulo)
            .Take(500)
            .Select(item => new
            {
                item.EXI_Existencia,
                item.EXI_LibroId,
                item.Libro.LIB_Codigo,
                item.Libro.LIB_Titulo,
                item.Almacen.ALM_Nombre,
                item.EXI_CantidadDisponible,
                item.EXI_StockMinimo,
                item.EXI_StockMaximo
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row =>
            {
                string level = Classify(row.EXI_CantidadDisponible, row.EXI_StockMinimo, row.EXI_StockMaximo);
                decimal target = row.EXI_StockMaximo > 0 ? row.EXI_StockMaximo : row.EXI_StockMinimo * 2;

                return new StockAlertResponse
                {
                    StockId = row.EXI_Existencia,
                    BookId = row.EXI_LibroId,
                    BookCode = row.LIB_Codigo,
                    Title = row.LIB_Titulo,
                    Warehouse = row.ALM_Nombre,
                    Available = row.EXI_CantidadDisponible,
                    Minimum = row.EXI_StockMinimo,
                    Maximum = row.EXI_StockMaximo,
                    Level = level,
                    LevelLabel = StockStatus.Label(level),
                    SuggestedOrder = level == StockStatus.AboveMaximum ? 0 : Math.Max(target - row.EXI_CantidadDisponible, 0)
                };
            })
            .OrderBy(alert => alert.Level switch
            {
                StockStatus.OutOfStock => 0,
                StockStatus.BelowMinimum => 1,
                _ => 2
            })
            .ToArray();
    }

    public async Task<StockAlertSummaryResponse> GetAlertSummaryAsync(CancellationToken cancellationToken = default)
    {
        IQueryable<PB_EXISTENCIA> stock = _context.Existencias.AsNoTracking();

        return new StockAlertSummaryResponse
        {
            OutOfStock = await FilterByStatus(stock, StockStatus.OutOfStock).CountAsync(cancellationToken),
            BelowMinimum = await FilterByStatus(stock, StockStatus.BelowMinimum).CountAsync(cancellationToken),
            AboveMaximum = await FilterByStatus(stock, StockStatus.AboveMaximum).CountAsync(cancellationToken)
        };
    }

    public async Task<PagedResponse<MovementListItemResponse>> GetMovementsAsync(
        MovementQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_MOVIMIENTO_INVENTARIO> movements = _context.MovimientosInventario.AsNoTracking();

        if (!query.IncludeAnnulled)
        {
            movements = movements.Where(item => !item.MOV_Anulado);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            string type = query.Type.Trim().ToUpperInvariant();
            movements = movements.Where(item => item.TipoMovimiento.TMI_Codigo == type);
        }

        if (query.WarehouseId.HasValue)
        {
            movements = movements.Where(item =>
                item.MOV_AlmacenOrigenId == query.WarehouseId || item.MOV_AlmacenDestinoId == query.WarehouseId);
        }

        if (query.BookId.HasValue)
        {
            movements = movements.Where(item => item.Detalles.Any(line => line.MDE_LibroId == query.BookId));
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            movements = movements.Where(item => item.MOV_Fecha >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            movements = movements.Where(item => item.MOV_Fecha < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            movements = movements.Where(item =>
                item.MOV_Numero.Contains(value) ||
                (item.MOV_DocumentoReferencia != null && item.MOV_DocumentoReferencia.Contains(value)) ||
                (item.Empleado != null &&
                 (item.Empleado.EMP_Codigo.Contains(value) ||
                  (item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                   item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value))));
        }

        int totalItems = await movements.CountAsync(cancellationToken);

        List<MovementListItemResponse> items = await movements
            .OrderByDescending(item => item.MOV_Fecha)
            .ThenByDescending(item => item.MOV_MovimientoInventario)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new MovementListItemResponse
            {
                Id = item.MOV_MovimientoInventario,
                Number = item.MOV_Numero,
                TypeCode = item.TipoMovimiento.TMI_Codigo,
                Type = item.TipoMovimiento.TMI_Nombre,
                Nature = item.TipoMovimiento.TMI_Naturaleza,
                Date = item.MOV_Fecha,
                Warehouse = item.AlmacenDestino != null ? item.AlmacenDestino.ALM_Nombre : item.AlmacenOrigen!.ALM_Nombre,
                DocumentReference = item.MOV_DocumentoReferencia,
                Employee = item.Empleado != null
                    ? item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido
                    : null,
                EmployeeCode = item.Empleado != null ? item.Empleado.EMP_Codigo : null,
                Units = item.Detalles.Sum(line => line.MDE_Cantidad),
                Annulled = item.MOV_Anulado
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<MovementListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<MovementResponse?> GetMovementAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        MovementResponse? response = await _context.MovimientosInventario
            .AsNoTracking()
            .Where(item => item.MOV_MovimientoInventario == id)
            .Select(item => new MovementResponse
            {
                Id = item.MOV_MovimientoInventario,
                Number = item.MOV_Numero,
                TypeCode = item.TipoMovimiento.TMI_Codigo,
                Type = item.TipoMovimiento.TMI_Nombre,
                Nature = item.TipoMovimiento.TMI_Naturaleza,
                Date = item.MOV_Fecha,
                SourceWarehouse = item.AlmacenOrigen != null ? item.AlmacenOrigen.ALM_Nombre : null,
                TargetWarehouse = item.AlmacenDestino != null ? item.AlmacenDestino.ALM_Nombre : null,
                DocumentReference = item.MOV_DocumentoReferencia,
                Employee = item.Empleado != null
                    ? item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                      item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                      " (" + item.Empleado.EMP_Codigo + ")"
                    : null,
                Notes = item.MOV_Observaciones,
                Annulled = item.MOV_Anulado,
                AnnulledAt = item.MOV_FechaAnulacion,
                AnnulmentReason = item.MOV_MotivoAnulacion,
                ReceptionId = item.MOV_RecepcionCompraId,
                Lines = item.Detalles
                    .OrderBy(line => line.MDE_MovimientoInventarioDetalle)
                    .Select(line => new MovementLineResponse
                    {
                        BookId = line.MDE_LibroId,
                        BookCode = line.Libro.LIB_Codigo,
                        Title = line.Libro.LIB_Titulo,
                        LotNumber = line.Lote != null ? line.Lote.LOT_Numero : null,
                        Quantity = line.MDE_Cantidad,
                        UnitCost = line.MDE_CostoUnitario,
                        TotalCost = line.MDE_CostoTotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null && !includeCosts)
        {
            foreach (MovementLineResponse line in response.Lines)
            {
                line.UnitCost = null;
                line.TotalCost = null;
            }
        }

        return response;
    }

    public async Task<OperationResult<KardexResponse>> GetKardexAsync(
        KardexQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        var book = await _context.Libros
            .AsNoTracking()
            .Where(item => item.LIB_Libro == query.BookId)
            .Select(item => new { item.LIB_Libro, item.LIB_Codigo, item.LIB_Titulo })
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null)
        {
            return OperationResult<KardexResponse>.Missing("El libro solicitado no existe.");
        }

        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
        {
            return OperationResult<KardexResponse>.Failure("La fecha inicial no puede ser posterior a la final.");
        }

        string? warehouse = query.WarehouseId.HasValue
            ? await _context.Almacenes
                .Where(item => item.ALM_Almacen == query.WarehouseId)
                .Select(item => item.ALM_Nombre)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (query.WarehouseId.HasValue && warehouse is null)
        {
            return OperationResult<KardexResponse>.Missing("El almacén solicitado no existe.");
        }

        int? warehouseId = query.WarehouseId;

        // Cada línea de movimiento es una entrada (destino) o una salida
        // (origen); en una transferencia sin filtro de almacén se compensan.
        var lines = _context.MovimientosInventarioDetalles
            .AsNoTracking()
            .Where(line => line.MDE_LibroId == book.LIB_Libro && !line.MovimientoInventario.MOV_Anulado)
            .Select(line => new
            {
                line.MDE_MovimientoInventarioId,
                line.MovimientoInventario.MOV_Numero,
                line.MovimientoInventario.MOV_Fecha,
                Type = line.MovimientoInventario.TipoMovimiento.TMI_Nombre,
                line.MovimientoInventario.TipoMovimiento.TMI_Naturaleza,
                line.MovimientoInventario.MOV_AlmacenOrigenId,
                line.MovimientoInventario.MOV_AlmacenDestinoId,
                OriginName = line.MovimientoInventario.AlmacenOrigen != null ? line.MovimientoInventario.AlmacenOrigen.ALM_Nombre : null,
                TargetName = line.MovimientoInventario.AlmacenDestino != null ? line.MovimientoInventario.AlmacenDestino.ALM_Nombre : null,
                line.MovimientoInventario.MOV_DocumentoReferencia,
                LotNumber = line.Lote != null ? line.Lote.LOT_Numero : null,
                line.MDE_Cantidad,
                line.MDE_CostoUnitario
            })
            .Where(line => warehouseId == null ||
                           line.MOV_AlmacenOrigenId == warehouseId ||
                           line.MOV_AlmacenDestinoId == warehouseId);

        DateTime? from = query.From is { } fromDate ? BusinessTime.StartOfDayUtc(fromDate) : null;
        DateTime? to = query.To is { } toDate ? BusinessTime.EndOfDayUtcExclusive(toDate) : null;

        var rows = await lines
            .Where(line => to == null || line.MOV_Fecha < to)
            .OrderBy(line => line.MOV_Fecha)
            .ThenBy(line => line.MDE_MovimientoInventarioId)
            .ToListAsync(cancellationToken);

        decimal balance = 0;
        decimal opening = 0;
        List<KardexEntryResponse> entries = [];

        foreach (var row in rows)
        {
            (decimal inQty, decimal outQty) = row.TMI_Naturaleza switch
            {
                > 0 => (row.MDE_Cantidad, 0m),
                < 0 => (0m, row.MDE_Cantidad),
                _ when warehouseId == row.MOV_AlmacenDestinoId => (row.MDE_Cantidad, 0m),
                _ when warehouseId == row.MOV_AlmacenOrigenId => (0m, row.MDE_Cantidad),
                _ => (0m, 0m)
            };

            balance += inQty - outQty;

            if (from.HasValue && row.MOV_Fecha < from.Value)
            {
                opening = balance;
                continue;
            }

            entries.Add(new KardexEntryResponse
            {
                MovementId = row.MDE_MovimientoInventarioId,
                MovementNumber = row.MOV_Numero,
                Date = row.MOV_Fecha,
                Type = row.Type,
                Warehouse = (inQty > 0 ? row.TargetName : row.OriginName) ?? row.TargetName ?? row.OriginName ?? string.Empty,
                DocumentReference = row.MOV_DocumentoReferencia,
                LotNumber = row.LotNumber,
                In = inQty,
                Out = outQty,
                Balance = balance,
                UnitCost = includeCosts ? row.MDE_CostoUnitario : null
            });
        }

        return OperationResult<KardexResponse>.Success(new KardexResponse
        {
            BookId = book.LIB_Libro,
            BookCode = book.LIB_Codigo,
            Title = book.LIB_Titulo,
            Warehouse = warehouse,
            From = query.From,
            To = query.To,
            OpeningBalance = opening,
            TotalIn = entries.Sum(entry => entry.In),
            TotalOut = entries.Sum(entry => entry.Out),
            ClosingBalance = balance,
            Entries = entries.TakeLast(KardexLimit).ToArray(),
            Truncated = entries.Count > KardexLimit
        });
    }

    public async Task<PagedResponse<AdjustmentListItemResponse>> GetAdjustmentsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => 10, > 100 => 100, _ => pageSize };

        IQueryable<PB_AJUSTE_INVENTARIO> adjustments = _context.AjustesInventario.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();
            adjustments = adjustments.Where(item =>
                item.AJU_Numero.Contains(value) ||
                item.Empleado.EMP_Codigo.Contains(value) ||
                (item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                 item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value));
        }

        int totalItems = await adjustments.CountAsync(cancellationToken);

        List<AdjustmentListItemResponse> items = await adjustments
            .OrderByDescending(item => item.AJU_Fecha)
            .ThenByDescending(item => item.AJU_AjusteInventario)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AdjustmentListItemResponse
            {
                Id = item.AJU_AjusteInventario,
                Number = item.AJU_Numero,
                Warehouse = item.Almacen.ALM_Nombre,
                Reason = item.MotivoAjuste.MAJ_Nombre,
                Nature = item.MotivoAjuste.MAJ_Naturaleza,
                Date = item.AJU_Fecha,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                EmployeeCode = item.Empleado.EMP_Codigo,
                Units = item.Detalles.Sum(line => line.AID_Diferencia)
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<AdjustmentListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<AdjustmentResponse?> GetAdjustmentAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        AdjustmentResponse? response = await _context.AjustesInventario
            .AsNoTracking()
            .Where(item => item.AJU_AjusteInventario == id)
            .Select(item => new AdjustmentResponse
            {
                Id = item.AJU_AjusteInventario,
                Number = item.AJU_Numero,
                Warehouse = item.Almacen.ALM_Nombre,
                Reason = item.MotivoAjuste.MAJ_Nombre,
                Nature = item.MotivoAjuste.MAJ_Naturaleza,
                Date = item.AJU_Fecha,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                           " (" + item.Empleado.EMP_Codigo + ")",
                MovementNumber = item.MovimientoInventario != null ? item.MovimientoInventario.MOV_Numero : null,
                Notes = item.AJU_Observaciones,
                Lines = item.Detalles
                    .OrderBy(line => line.AID_AjusteInventarioDetalle)
                    .Select(line => new AdjustmentLineResponse
                    {
                        BookId = line.AID_LibroId,
                        BookCode = line.Libro.LIB_Codigo,
                        Title = line.Libro.LIB_Titulo,
                        SystemQuantity = line.AID_CantidadSistema,
                        PhysicalQuantity = line.AID_CantidadFisica,
                        Difference = line.AID_Diferencia,
                        UnitCost = line.AID_CostoUnitario,
                        Notes = line.AID_Observaciones
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null && !includeCosts)
        {
            foreach (AdjustmentLineResponse line in response.Lines)
            {
                line.UnitCost = null;
            }
        }

        return response;
    }

    public async Task<OperationResult<AdjustmentResponse>> CreateAdjustmentAsync(
        SaveAdjustmentRequest request,
        int authenticatedUserId,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<AdjustmentResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        var reason = await _context.MotivosAjuste
            .AsNoTracking()
            .Where(item => item.MAJ_MotivoAjuste == request.ReasonId && item.MAJ_Activo)
            .Select(item => new { item.MAJ_MotivoAjuste, item.MAJ_Nombre, item.MAJ_Naturaleza, item.MAJ_RequiereObservacion })
            .FirstOrDefaultAsync(cancellationToken);

        List<string> errors = [];

        if (reason is null)
        {
            errors.Add("El motivo de ajuste no existe o está inactivo.");
        }
        else if (reason.MAJ_RequiereObservacion && (request.Notes?.Trim().Length ?? 0) < 5)
        {
            errors.Add($"El motivo \"{reason.MAJ_Nombre}\" requiere observaciones (mínimo 5 caracteres).");
        }

        int? warehouseId = request.WarehouseId.HasValue
            ? await _context.Almacenes
                .Where(item => item.ALM_Almacen == request.WarehouseId && item.ALM_Activo)
                .Select(item => (int?)item.ALM_Almacen)
                .FirstOrDefaultAsync(cancellationToken)
            : await _context.Almacenes
                .Where(item => item.ALM_EsPrincipal && item.ALM_Activo)
                .Select(item => (int?)item.ALM_Almacen)
                .FirstOrDefaultAsync(cancellationToken);

        if (warehouseId is null)
        {
            errors.Add("El almacén seleccionado no existe o está inactivo.");
        }

        int[] bookIds = request.Lines.Select(line => line.BookId!.Value).ToArray();

        Dictionary<int, BookData> books = (await _context.Libros
                .AsNoTracking()
                .Where(book => bookIds.Contains(book.LIB_Libro))
                .Select(book => new { book.LIB_Libro, book.LIB_Titulo, book.LIB_CostoReferencia })
                .ToListAsync(cancellationToken))
            .ToDictionary(book => book.LIB_Libro, book => new BookData(book.LIB_Titulo, book.LIB_CostoReferencia));

        string[] missing = bookIds.Where(id => !books.ContainsKey(id)).Select(id => id.ToString()).ToArray();

        if (missing.Length > 0)
        {
            errors.Add($"Los libros con id {string.Join(", ", missing)} no existen.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<AdjustmentResponse>.Failure(errors.ToArray());
        }

        bool positive = reason!.MAJ_Naturaleza > 0;

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    DateTime now = DateTime.UtcNow;
                    List<string> shortages = [];

                    PB_AJUSTE_INVENTARIO adjustment = new()
                    {
                        AJU_Numero = await _numbers.NextAsync(DocumentTypes.InventoryAdjustment, token),
                        AJU_AlmacenId = warehouseId!.Value,
                        AJU_MotivoAjusteId = reason.MAJ_MotivoAjuste,
                        AJU_EmpleadoId = employee.Id,
                        AJU_Fecha = now,
                        AJU_Observaciones = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                        AJU_Procesado = true,
                        AJU_FechaProcesado = now,
                        AJU_FechaCreacion = now
                    };

                    PB_MOVIMIENTO_INVENTARIO movement = new()
                    {
                        MOV_Numero = await _numbers.NextAsync(DocumentTypes.InventoryMovement, token),
                        MOV_TipoMovimientoId = await _context.TiposMovimientoInventario
                            .Where(type => type.TMI_Codigo == (positive ? PositiveAdjustmentType : NegativeAdjustmentType))
                            .Select(type => type.TMI_TipoMovimientoInventario)
                            .SingleAsync(token),
                        MOV_AlmacenDestinoId = positive ? warehouseId : null,
                        MOV_AlmacenOrigenId = positive ? null : warehouseId,
                        MOV_EmpleadoId = employee.Id,
                        MOV_Fecha = now,
                        MOV_DocumentoReferencia = $"{adjustment.AJU_Numero} / {reason.MAJ_Nombre}",
                        MOV_Observaciones = adjustment.AJU_Observaciones,
                        MOV_Procesado = true,
                        MOV_FechaProcesado = now,
                        MOV_FechaCreacion = now
                    };

                    foreach (AdjustmentLineRequest line in request.Lines)
                    {
                        int bookId = line.BookId!.Value;
                        decimal quantity = line.Quantity!.Value;

                        PB_EXISTENCIA? stock = await _context.Existencias.FirstOrDefaultAsync(
                            item => item.EXI_AlmacenId == warehouseId && item.EXI_LibroId == bookId, token);

                        decimal systemQuantity = stock?.EXI_CantidadDisponible ?? 0;
                        decimal free = systemQuantity - (stock?.EXI_CantidadReservada ?? 0);

                        if (!positive && quantity > free)
                        {
                            shortages.Add($"\"{books[bookId].Title}\": se quieren retirar {quantity:0} y hay {free:0} disponibles.");
                            continue;
                        }

                        if (stock is null)
                        {
                            stock = new PB_EXISTENCIA { EXI_AlmacenId = warehouseId!.Value, EXI_LibroId = bookId, EXI_FechaCreacion = now };
                            _context.Existencias.Add(stock);
                        }

                        decimal unitCost;

                        if (positive)
                        {
                            unitCost = line.UnitCost ?? await LastLotCostAsync(warehouseId!.Value, bookId, token) ?? books[bookId].ReferenceCost;

                            PB_LOTE lot = new()
                            {
                                LOT_AlmacenId = warehouseId!.Value,
                                LOT_LibroId = bookId,
                                LOT_Numero = adjustment.AJU_Numero,
                                LOT_FechaIngreso = BusinessTime.Today,
                                LOT_CantidadInicial = quantity,
                                LOT_CantidadActual = quantity,
                                LOT_CostoUnitario = unitCost,
                                LOT_Activo = true,
                                LOT_FechaCreacion = now
                            };

                            movement.Detalles.Add(new PB_MOVIMIENTO_INVENTARIO_DETALLE
                            {
                                MDE_LibroId = bookId,
                                Lote = lot,
                                MDE_Cantidad = quantity,
                                MDE_CostoUnitario = unitCost,
                                MDE_CostoTotal = LineAmounts.Round(quantity * unitCost)
                            });

                            stock.EXI_CantidadDisponible += quantity;
                        }
                        else
                        {
                            unitCost = await ConsumeLotsAsync(movement, warehouseId!.Value, bookId, quantity, books[bookId].ReferenceCost, token);
                            stock.EXI_CantidadDisponible -= quantity;
                        }

                        stock.EXI_FechaUltimoMovimiento = now;
                        stock.EXI_FechaModificacion = now;

                        decimal difference = positive ? quantity : -quantity;

                        adjustment.Detalles.Add(new PB_AJUSTE_INVENTARIO_DETALLE
                        {
                            AID_LibroId = bookId,
                            AID_CantidadSistema = systemQuantity,
                            AID_CantidadFisica = systemQuantity + difference,
                            AID_Diferencia = difference,
                            AID_CostoUnitario = unitCost,
                            AID_Observaciones = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
                        });
                    }

                    if (shortages.Count > 0)
                    {
                        return OperationResult<AdjustmentResponse>.Conflict(shortages.ToArray());
                    }

                    adjustment.MovimientoInventario = movement;
                    _context.MovimientosInventario.Add(movement);
                    _context.AjustesInventario.Add(adjustment);

                    await _context.SaveChangesAsync(token);

                    return OperationResult<AdjustmentResponse>.Success(
                        (await GetAdjustmentAsync(adjustment.AJU_AjusteInventario, includeCosts, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<AdjustmentResponse>.Conflict(
                "El inventario cambió mientras se guardaba el ajuste. Intente nuevamente.");
        }
    }

    /// <summary>
    /// Retira la cantidad de los lotes más antiguos (PEPS) y agrega una línea
    /// de movimiento por lote. Devuelve el costo promedio retirado. Las ventas
    /// lo usan para la salida de inventario.
    /// </summary>
    internal async Task<decimal> ConsumeLotsAsync(
        PB_MOVIMIENTO_INVENTARIO movement,
        int warehouseId,
        int bookId,
        decimal quantity,
        decimal fallbackCost,
        CancellationToken cancellationToken)
    {
        List<PB_LOTE> lots = await _context.Lotes
            .Where(lot => lot.LOT_AlmacenId == warehouseId && lot.LOT_LibroId == bookId && lot.LOT_Activo && lot.LOT_CantidadActual > 0)
            .OrderBy(lot => lot.LOT_FechaIngreso)
            .ThenBy(lot => lot.LOT_Lote)
            .ToListAsync(cancellationToken);

        decimal remaining = quantity;
        decimal totalCost = 0;

        foreach (PB_LOTE lot in lots)
        {
            if (remaining == 0)
            {
                break;
            }

            decimal taken = Math.Min(remaining, lot.LOT_CantidadActual);

            lot.LOT_CantidadActual -= taken;
            lot.LOT_Activo = lot.LOT_CantidadActual > 0;
            lot.LOT_FechaModificacion = DateTime.UtcNow;
            remaining -= taken;
            totalCost += taken * lot.LOT_CostoUnitario;

            movement.Detalles.Add(new PB_MOVIMIENTO_INVENTARIO_DETALLE
            {
                MDE_LibroId = bookId,
                Lote = lot,
                MDE_Cantidad = taken,
                MDE_CostoUnitario = lot.LOT_CostoUnitario,
                MDE_CostoTotal = LineAmounts.Round(taken * lot.LOT_CostoUnitario)
            });
        }

        if (remaining > 0)
        {
            // Existencia sin lote (por ejemplo, cargada antes de usar lotes).
            totalCost += remaining * fallbackCost;

            movement.Detalles.Add(new PB_MOVIMIENTO_INVENTARIO_DETALLE
            {
                MDE_LibroId = bookId,
                MDE_Cantidad = remaining,
                MDE_CostoUnitario = fallbackCost,
                MDE_CostoTotal = LineAmounts.Round(remaining * fallbackCost)
            });
        }

        return decimal.Round(totalCost / quantity, 4, MidpointRounding.AwayFromZero);
    }

    private Task<decimal?> LastLotCostAsync(int warehouseId, int bookId, CancellationToken cancellationToken) =>
        _context.Lotes
            .Where(lot => lot.LOT_AlmacenId == warehouseId && lot.LOT_LibroId == bookId)
            .OrderByDescending(lot => lot.LOT_FechaIngreso)
            .ThenByDescending(lot => lot.LOT_Lote)
            .Select(lot => (decimal?)lot.LOT_CostoUnitario)
            .FirstOrDefaultAsync(cancellationToken);

    private Task<string> BookCodeAsync(int bookId, CancellationToken cancellationToken) =>
        _context.Libros.Where(book => book.LIB_Libro == bookId).Select(book => book.LIB_Codigo).SingleAsync(cancellationToken);

    /// <summary>Agotado, bajo el mínimo o sobre el máximo.</summary>
    private static IQueryable<PB_EXISTENCIA> AlertsQuery(IQueryable<PB_EXISTENCIA> stock) =>
        stock.Where(item =>
            item.EXI_CantidadDisponible == 0 ||
            (item.EXI_StockMinimo > 0 && item.EXI_CantidadDisponible <= item.EXI_StockMinimo) ||
            (item.EXI_StockMaximo > 0 && item.EXI_CantidadDisponible > item.EXI_StockMaximo));

    private static IQueryable<PB_EXISTENCIA> FilterByStatus(IQueryable<PB_EXISTENCIA> stock, string? status) =>
        status?.Trim().ToUpperInvariant() switch
        {
            StockStatus.OutOfStock => stock.Where(item => item.EXI_CantidadDisponible == 0),
            StockStatus.BelowMinimum => stock.Where(item =>
                item.EXI_CantidadDisponible > 0 &&
                item.EXI_StockMinimo > 0 &&
                item.EXI_CantidadDisponible <= item.EXI_StockMinimo),
            StockStatus.AboveMaximum => stock.Where(item =>
                item.EXI_StockMaximo > 0 && item.EXI_CantidadDisponible > item.EXI_StockMaximo),
            StockStatus.Normal => stock.Where(item =>
                item.EXI_CantidadDisponible > 0 &&
                (item.EXI_StockMinimo == 0 || item.EXI_CantidadDisponible > item.EXI_StockMinimo) &&
                (item.EXI_StockMaximo == 0 || item.EXI_CantidadDisponible <= item.EXI_StockMaximo)),
            _ => stock
        };

    public static string Classify(decimal available, decimal minimum, decimal maximum) =>
        available == 0 ? StockStatus.OutOfStock
        : minimum > 0 && available <= minimum ? StockStatus.BelowMinimum
        : maximum > 0 && available > maximum ? StockStatus.AboveMaximum
        : StockStatus.Normal;

    private sealed record BookData(string Title, decimal ReferenceCost);
}
