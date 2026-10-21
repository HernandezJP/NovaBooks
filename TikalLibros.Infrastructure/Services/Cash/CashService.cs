using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Sales;
using TikalLibros.Application.Interfaces;
using TikalLibros.Domain.Entities.Cash;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.Audit;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services.Cash;

/// <summary>
/// Caja: apertura con monto inicial, ingresos y egresos manuales, y cierre
/// con arqueo. Los cobros de ventas entran como movimientos VENTA desde el
/// servicio de ventas. Cada operación queda en la auditoría.
/// </summary>
public sealed class CashService : ICashService
{
    public const string NoOpenSessionMessage =
        "No tiene una caja abierta. Abra una caja para cobrar o registrar movimientos.";

    private const string LocalCurrency = "GTQ";
    private const string CashEntity = "AperturaCaja";

    private readonly AppDbContext _context;
    private readonly DocumentNumberService _numbers;
    private readonly CurrentEmployeeResolver _employees;
    private readonly AuditTrail _audit;

    public CashService(
        AppDbContext context,
        DocumentNumberService numbers,
        CurrentEmployeeResolver employees,
        AuditTrail audit)
    {
        _context = context;
        _numbers = numbers;
        _employees = employees;
        _audit = audit;
    }

    public async Task<CashCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default)
    {
        return new CashCatalogsResponse
        {
            CashRegisters = await GetCashRegistersAsync(cancellationToken),
            MovementTypes = await _context.TiposMovimientoCaja
                .AsNoTracking()
                .Where(type => type.TMC_Activo && type.TMC_Codigo != CashMovementTypes.Sale)
                .OrderByDescending(type => type.TMC_Naturaleza)
                .ThenBy(type => type.TMC_Nombre)
                .Select(type => new CashMovementTypeResponse
                {
                    Code = type.TMC_Codigo,
                    Name = type.TMC_Nombre,
                    Nature = type.TMC_Naturaleza
                })
                .ToListAsync(cancellationToken),
            PaymentMethods = await _context.MetodosPago
                .AsNoTracking()
                .Where(method => method.MPA_Activo && method.MPA_Codigo != PaymentMethodCode.Credit)
                .OrderBy(method => method.MPA_Nombre)
                .Select(method => new CatalogOptionResponse { Id = method.MPA_MetodoPago, Code = method.MPA_Codigo, Name = method.MPA_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    private async Task<IReadOnlyCollection<CashRegisterResponse>> GetCashRegistersAsync(CancellationToken cancellationToken)
    {
        return await _context.Cajas
            .AsNoTracking()
            .Where(register => register.CAJ_Activo)
            .OrderBy(register => register.CAJ_Codigo)
            .Select(register => new
            {
                register.CAJ_Caja,
                register.CAJ_Codigo,
                register.CAJ_Nombre,
                Branch = register.Sucursal.SUC_Nombre,
                Open = register.Aperturas
                    .Where(session => session.ACA_Abierta)
                    .Select(session => new
                    {
                        session.ACA_AperturaCaja,
                        session.ACA_FechaApertura,
                        Name = session.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                               session.Empleado.Persona.PersonaNatural.PNA_PrimerApellido
                    })
                    .FirstOrDefault()
            })
            .Select(item => new CashRegisterResponse
            {
                Id = item.CAJ_Caja,
                Code = item.CAJ_Codigo,
                Name = item.CAJ_Nombre,
                Branch = item.Branch,
                OpenSessionId = item.Open != null ? item.Open.ACA_AperturaCaja : null,
                OpenedAt = item.Open != null ? item.Open.ACA_FechaApertura : null,
                OpenedBy = item.Open != null ? item.Open.Name : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<CashSessionListItemResponse>> GetSessionsAsync(
        CashSessionQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize switch { < 1 => 10, > 100 => 100, _ => query.PageSize };

        IQueryable<PB_APERTURA_CAJA> sessions = _context.AperturasCaja.AsNoTracking();

        if (query.CashRegisterId.HasValue)
        {
            sessions = sessions.Where(item => item.ACA_CajaId == query.CashRegisterId.Value);
        }

        if (query.IsOpen.HasValue)
        {
            sessions = sessions.Where(item => item.ACA_Abierta == query.IsOpen.Value);
        }

        if (query.From.HasValue)
        {
            DateTime from = BusinessTime.StartOfDayUtc(query.From.Value);
            sessions = sessions.Where(item => item.ACA_FechaApertura >= from);
        }

        if (query.To.HasValue)
        {
            DateTime to = BusinessTime.EndOfDayUtcExclusive(query.To.Value);
            sessions = sessions.Where(item => item.ACA_FechaApertura < to);
        }

        int totalItems = await sessions.CountAsync(cancellationToken);

        List<CashSessionListItemResponse> items = await sessions
            .OrderByDescending(item => item.ACA_FechaApertura)
            .ThenByDescending(item => item.ACA_AperturaCaja)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new CashSessionListItemResponse
            {
                Id = item.ACA_AperturaCaja,
                CashRegister = item.Caja.CAJ_Nombre,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                EmployeeCode = item.Empleado.EMP_Codigo,
                OpenedAt = item.ACA_FechaApertura,
                IsOpen = item.ACA_Abierta,
                ClosedAt = item.CierreCaja != null ? item.CierreCaja.CCA_FechaCierre : null,
                InitialAmount = item.ACA_MontoInicial,
                CountedCash = item.CierreCaja != null ? item.CierreCaja.CCA_MontoContado : null,
                Difference = item.CierreCaja != null ? item.CierreCaja.CCA_Diferencia : null
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<CashSessionListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<CashSessionResponse?> GetSessionAsync(
        int id,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        return await BuildResponseAsync(id, employee?.Id, cancellationToken);
    }

    public async Task<CashSessionResponse?> GetCurrentSessionAsync(
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return null;
        }

        int? sessionId = await _context.AperturasCaja
            .Where(item => item.ACA_Abierta && item.ACA_EmpleadoId == employee.Id)
            .Select(item => (int?)item.ACA_AperturaCaja)
            .FirstOrDefaultAsync(cancellationToken);

        return sessionId is null ? null : await BuildResponseAsync(sessionId.Value, employee.Id, cancellationToken);
    }

    /// <summary>Apertura abierta del empleado (la usan las ventas para cobrar).</summary>
    internal Task<PB_APERTURA_CAJA?> FindOpenSessionAsync(int employeeId, CancellationToken cancellationToken) =>
        _context.AperturasCaja.FirstOrDefaultAsync(
            item => item.ACA_Abierta && item.ACA_EmpleadoId == employeeId,
            cancellationToken);

    public async Task<OperationResult<CashSessionResponse>> OpenAsync(
        OpenCashSessionRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<CashSessionResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        PB_CAJA? register = await _context.Cajas
            .FirstOrDefaultAsync(item => item.CAJ_Caja == request.CashRegisterId, cancellationToken);

        if (register is null)
        {
            return OperationResult<CashSessionResponse>.Missing("La caja seleccionada no existe.");
        }

        if (!register.CAJ_Activo)
        {
            return OperationResult<CashSessionResponse>.Conflict($"La caja {register.CAJ_Nombre} está inactiva.");
        }

        var openByRegister = await _context.AperturasCaja
            .Where(item => item.ACA_Abierta && item.ACA_CajaId == register.CAJ_Caja)
            .Select(item => new
            {
                Name = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                       item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (openByRegister is not null)
        {
            return OperationResult<CashSessionResponse>.Conflict(
                $"La caja {register.CAJ_Nombre} ya está abierta por {openByRegister.Name}. Debe cerrarse antes de abrirla de nuevo.");
        }

        string? ownOpen = await _context.AperturasCaja
            .Where(item => item.ACA_Abierta && item.ACA_EmpleadoId == employee.Id)
            .Select(item => item.Caja.CAJ_Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownOpen is not null)
        {
            return OperationResult<CashSessionResponse>.Conflict(
                $"Ya tiene abierta la caja {ownOpen}. Ciérrela antes de abrir otra.");
        }

        PB_APERTURA_CAJA session = new()
        {
            ACA_CajaId = register.CAJ_Caja,
            ACA_EmpleadoId = employee.Id,
            ACA_MonedaId = await LocalCurrencyIdAsync(cancellationToken),
            ACA_FechaApertura = DateTime.UtcNow,
            ACA_MontoInicial = request.InitialAmount!.Value,
            ACA_Observaciones = Clean(request.Notes),
            ACA_Abierta = true
        };

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    _context.AperturasCaja.Add(session);
                    await _context.SaveChangesAsync(token);

                    _audit.Add(
                        AuditActions.Open,
                        CashEntity,
                        session.ACA_AperturaCaja.ToString(),
                        $"Abrió la caja {register.CAJ_Nombre} con Q{session.ACA_MontoInicial:N2}.",
                        [AuditChange.Of("Monto inicial", null, session.ACA_MontoInicial)]);
                    await _context.SaveChangesAsync(token);

                    return OperationResult<CashSessionResponse>.Success(
                        (await BuildResponseAsync(session.ACA_AperturaCaja, employee.Id, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // Otra persona abrió la misma caja al mismo tiempo.
            return OperationResult<CashSessionResponse>.Conflict(
                $"La caja {register.CAJ_Nombre} se abrió al mismo tiempo desde otra sesión. Actualice la pantalla.");
        }
    }

    public async Task<OperationResult<CashSessionResponse>> RegisterMovementAsync(
        int sessionId,
        CashMovementRequest request,
        int authenticatedUserId,
        CashPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<CashSessionResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        PB_TIPO_MOVIMIENTO_CAJA? type = await _context.TiposMovimientoCaja
            .FirstOrDefaultAsync(item => item.TMC_Codigo == request.TypeCode && item.TMC_Activo, cancellationToken);

        if (type is null)
        {
            return OperationResult<CashSessionResponse>.Failure("El tipo de movimiento no existe o está inactivo.");
        }

        if (type.TMC_Codigo == CashMovementTypes.Sale)
        {
            return OperationResult<CashSessionResponse>.Failure(
                "Los ingresos por venta los registra el sistema al cobrar; no se registran a mano.");
        }

        if (type.TMC_Naturaleza > 0 && !permissions.CanRegisterIncome)
        {
            return OperationResult<CashSessionResponse>.Forbidden("No tiene permiso para registrar ingresos de caja.");
        }

        if (type.TMC_Naturaleza < 0 && !permissions.CanRegisterExpense)
        {
            return OperationResult<CashSessionResponse>.Forbidden("No tiene permiso para registrar egresos de caja.");
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                PB_APERTURA_CAJA? session = await _context.AperturasCaja
                    .Include(item => item.Caja)
                    .FirstOrDefaultAsync(item => item.ACA_AperturaCaja == sessionId, token);

                OperationResult<CashSessionResponse>? invalid = CheckOwnOpenSession(session, employee);

                if (invalid is not null)
                {
                    return invalid;
                }

                decimal amount = request.Amount!.Value;

                if (type.TMC_Naturaleza < 0)
                {
                    decimal available = await ExpectedCashAsync(session!, token);

                    if (amount > available)
                    {
                        return OperationResult<CashSessionResponse>.Conflict(
                            $"No hay suficiente efectivo en caja: hay Q{available:N2} y el egreso es de Q{amount:N2}.");
                    }
                }

                PB_MOVIMIENTO_CAJA movement = await AddMovementAsync(
                    session!, type, await CashMethodIdAsync(token), employee.Id, amount,
                    Clean(request.Concept), Clean(request.Reference), saleId: null, token);

                await _context.SaveChangesAsync(token);

                _audit.Add(
                    AuditActions.Create,
                    "MovimientoCaja",
                    movement.MCA_MovimientoCaja.ToString(),
                    $"{type.TMC_Nombre} {movement.MCA_Numero} por Q{amount:N2} en {session!.Caja.CAJ_Nombre}: {movement.MCA_Concepto}",
                    [AuditChange.Of("Monto", null, amount)]);
                await _context.SaveChangesAsync(token);

                return OperationResult<CashSessionResponse>.Success(
                    (await BuildResponseAsync(session.ACA_AperturaCaja, employee.Id, token))!);
            },
            cancellationToken);
    }

    public async Task<OperationResult<CashSessionResponse>> AnnulMovementAsync(
        int movementId,
        AnnulCashMovementRequest request,
        int authenticatedUserId,
        CashPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<CashSessionResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                PB_MOVIMIENTO_CAJA? movement = await _context.MovimientosCaja
                    .Include(item => item.AperturaCaja).ThenInclude(item => item.Caja)
                    .Include(item => item.TipoMovimientoCaja)
                    .FirstOrDefaultAsync(item => item.MCA_MovimientoCaja == movementId, token);

                if (movement is null)
                {
                    return OperationResult<CashSessionResponse>.Missing("El movimiento de caja no existe.");
                }

                if (movement.MCA_Anulado)
                {
                    return OperationResult<CashSessionResponse>.Conflict($"El movimiento {movement.MCA_Numero} ya está anulado.");
                }

                if (movement.MCA_VentaId is not null || movement.MCA_DevolucionVentaId is not null)
                {
                    return OperationResult<CashSessionResponse>.Conflict(
                        "Este movimiento pertenece a una venta; se revierte al anular la venta.");
                }

                if (!movement.AperturaCaja.ACA_Abierta)
                {
                    return OperationResult<CashSessionResponse>.Conflict(
                        "La caja de este movimiento ya se cerró; no puede anularse.");
                }

                if (movement.AperturaCaja.ACA_EmpleadoId != employee.Id && !permissions.CanSupervise)
                {
                    return OperationResult<CashSessionResponse>.Forbidden(
                        "Solo quien abrió la caja o un supervisor puede anular sus movimientos.");
                }

                // Anular un ingreso no puede dejar la caja con efectivo negativo.
                if (movement.TipoMovimientoCaja.TMC_Naturaleza > 0)
                {
                    decimal available = await ExpectedCashAsync(movement.AperturaCaja, token);

                    if (movement.MCA_MontoBase > available)
                    {
                        return OperationResult<CashSessionResponse>.Conflict(
                            $"No se puede anular: el efectivo en caja (Q{available:N2}) es menor que el ingreso (Q{movement.MCA_MontoBase:N2}).");
                    }
                }

                movement.MCA_Anulado = true;
                movement.MCA_FechaAnulacion = DateTime.UtcNow;
                movement.MCA_MotivoAnulacion = Clean(request.Reason);

                _audit.Add(
                    AuditActions.Annul,
                    "MovimientoCaja",
                    movement.MCA_MovimientoCaja.ToString(),
                    $"Anuló el movimiento {movement.MCA_Numero} de Q{movement.MCA_MontoBase:N2}: {movement.MCA_MotivoAnulacion}",
                    [AuditChange.Of("Anulado", false, true)]);

                await _context.SaveChangesAsync(token);

                return OperationResult<CashSessionResponse>.Success(
                    (await BuildResponseAsync(movement.MCA_AperturaCajaId, employee.Id, token))!);
            },
            cancellationToken);
    }

    public async Task<OperationResult<CashSessionResponse>> CloseAsync(
        int sessionId,
        CloseCashSessionRequest request,
        int authenticatedUserId,
        CashPermissionSet permissions,
        CancellationToken cancellationToken = default)
    {
        CurrentEmployee? employee = await _employees.FindAsync(authenticatedUserId, cancellationToken);

        if (employee is null)
        {
            return OperationResult<CashSessionResponse>.Conflict(CurrentEmployeeResolver.MissingEmployeeMessage);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_APERTURA_CAJA? session = await _context.AperturasCaja
                        .Include(item => item.Caja)
                        .FirstOrDefaultAsync(item => item.ACA_AperturaCaja == sessionId, token);

                    if (session is null)
                    {
                        return OperationResult<CashSessionResponse>.Missing("La apertura de caja no existe.");
                    }

                    if (!session.ACA_Abierta)
                    {
                        return OperationResult<CashSessionResponse>.Conflict($"La caja {session.Caja.CAJ_Nombre} ya está cerrada.");
                    }

                    if (session.ACA_EmpleadoId != employee.Id && !permissions.CanSupervise)
                    {
                        return OperationResult<CashSessionResponse>.Forbidden(
                            "Solo quien abrió la caja o un supervisor puede cerrarla.");
                    }

                    List<MethodTotal> totals = await MethodTotalsAsync(session.ACA_AperturaCaja, token);
                    decimal cashIncome = totals.Where(item => item.AffectsCash).Sum(item => item.Income);
                    decimal cashExpense = totals.Where(item => item.AffectsCash).Sum(item => item.Expense);
                    decimal expected = session.ACA_MontoInicial + cashIncome - cashExpense;
                    decimal counted = request.CountedCash!.Value;

                    Dictionary<int, decimal> otherCounts = request.Counts
                        .ToDictionary(item => item.PaymentMethodId!.Value, item => item.Counted!.Value);

                    List<string> errors = [];
                    var methods = await _context.MetodosPago
                        .Where(method => method.MPA_Activo)
                        .Select(method => new { method.MPA_MetodoPago, method.MPA_Nombre, method.MPA_AfectaEfectivo })
                        .ToListAsync(token);

                    foreach (int methodId in otherCounts.Keys)
                    {
                        var method = methods.FirstOrDefault(item => item.MPA_MetodoPago == methodId);

                        if (method is null)
                        {
                            errors.Add("Uno de los métodos de pago contados no existe o está inactivo.");
                        }
                        else if (method.MPA_AfectaEfectivo)
                        {
                            errors.Add($"El {method.MPA_Nombre.ToLowerInvariant()} se cuenta en «efectivo contado», no por separado.");
                        }
                    }

                    if (errors.Count > 0)
                    {
                        return OperationResult<CashSessionResponse>.Failure(errors.ToArray());
                    }

                    PB_CIERRE_CAJA closing = new()
                    {
                        CCA_AperturaCajaId = session.ACA_AperturaCaja,
                        CCA_EmpleadoId = employee.Id,
                        CCA_FechaCierre = DateTime.UtcNow,
                        CCA_MontoInicial = session.ACA_MontoInicial,
                        CCA_TotalIngresosEfectivo = cashIncome,
                        CCA_TotalEgresosEfectivo = cashExpense,
                        CCA_MontoEsperado = expected,
                        CCA_MontoContado = counted,
                        CCA_Diferencia = counted - expected,
                        CCA_Observaciones = Clean(request.Notes)
                    };

                    // Una línea por método usado o contado: efectivo contra el
                    // conteo físico; los demás contra lo que se indicó (si no se
                    // indicó, se toma como cuadrado).
                    foreach (var method in methods)
                    {
                        MethodTotal? total = totals.FirstOrDefault(item => item.MethodId == method.MPA_MetodoPago);
                        decimal system = method.MPA_AfectaEfectivo ? expected : total?.Net ?? 0m;
                        bool used = total is not null || otherCounts.ContainsKey(method.MPA_MetodoPago) || method.MPA_AfectaEfectivo;

                        if (!used)
                        {
                            continue;
                        }

                        decimal methodCounted = method.MPA_AfectaEfectivo
                            ? counted
                            : otherCounts.TryGetValue(method.MPA_MetodoPago, out decimal value) ? value : system;

                        closing.Detalles.Add(new PB_CIERRE_CAJA_DETALLE
                        {
                            CCD_MetodoPagoId = method.MPA_MetodoPago,
                            CCD_MontoSistema = Math.Max(system, 0m),
                            CCD_MontoContado = methodCounted,
                            CCD_Diferencia = methodCounted - Math.Max(system, 0m)
                        });
                    }

                    bool hasDifference = closing.Detalles.Any(line => line.CCD_Diferencia != 0);

                    if (hasDifference && (closing.CCA_Observaciones?.Length ?? 0) < 10)
                    {
                        return OperationResult<CashSessionResponse>.Failure(
                            $"El conteo no cuadra (diferencia en efectivo de Q{closing.CCA_Diferencia:N2}). " +
                            "Explique la diferencia en las observaciones (mínimo 10 caracteres).");
                    }

                    session.ACA_Abierta = false;
                    _context.CierresCaja.Add(closing);

                    _audit.Add(
                        AuditActions.Close,
                        CashEntity,
                        session.ACA_AperturaCaja.ToString(),
                        $"Cerró la caja {session.Caja.CAJ_Nombre}: esperado Q{expected:N2}, contado Q{counted:N2}, diferencia Q{closing.CCA_Diferencia:N2}.",
                        [
                            AuditChange.Of("Abierta", true, false),
                            AuditChange.Of("Efectivo esperado", null, expected),
                            AuditChange.Of("Efectivo contado", null, counted),
                            AuditChange.Of("Diferencia", null, closing.CCA_Diferencia)
                        ]);

                    await _context.SaveChangesAsync(token);

                    return OperationResult<CashSessionResponse>.Success(
                        (await BuildResponseAsync(session.ACA_AperturaCaja, employee.Id, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return OperationResult<CashSessionResponse>.Conflict("La caja se cerró al mismo tiempo desde otra sesión.");
        }
    }

    /// <summary>
    /// Agrega un movimiento (sin guardar). Las ventas lo usan con el tipo
    /// VENTA y el método con que se cobró.
    /// </summary>
    internal async Task<PB_MOVIMIENTO_CAJA> AddMovementAsync(
        PB_APERTURA_CAJA session,
        PB_TIPO_MOVIMIENTO_CAJA type,
        int paymentMethodId,
        int employeeId,
        decimal amount,
        string? concept,
        string? reference,
        int? saleId,
        CancellationToken cancellationToken)
    {
        PB_MOVIMIENTO_CAJA movement = new()
        {
            MCA_Numero = await _numbers.NextAsync(DocumentTypes.CashMovement, cancellationToken),
            MCA_AperturaCajaId = session.ACA_AperturaCaja,
            MCA_TipoMovimientoCajaId = type.TMC_TipoMovimientoCaja,
            MCA_MetodoPagoId = paymentMethodId,
            MCA_MonedaId = session.ACA_MonedaId,
            MCA_EmpleadoId = employeeId,
            MCA_VentaId = saleId,
            MCA_Fecha = DateTime.UtcNow,
            MCA_TipoCambio = 1m,
            MCA_MontoMoneda = amount,
            MCA_MontoBase = amount,
            MCA_Concepto = concept,
            MCA_Referencia = reference
        };

        _context.MovimientosCaja.Add(movement);

        return movement;
    }

    /// <summary>Efectivo que debería haber: inicial + ingresos − egresos en efectivo.</summary>
    internal async Task<decimal> ExpectedCashAsync(PB_APERTURA_CAJA session, CancellationToken cancellationToken)
    {
        List<MethodTotal> totals = await MethodTotalsAsync(session.ACA_AperturaCaja, cancellationToken);

        return session.ACA_MontoInicial + totals.Where(item => item.AffectsCash).Sum(item => item.Net);
    }

    private static OperationResult<CashSessionResponse>? CheckOwnOpenSession(PB_APERTURA_CAJA? session, CurrentEmployee employee)
    {
        if (session is null)
        {
            return OperationResult<CashSessionResponse>.Missing("La apertura de caja no existe.");
        }

        if (!session.ACA_Abierta)
        {
            return OperationResult<CashSessionResponse>.Conflict($"La caja {session.Caja.CAJ_Nombre} está cerrada.");
        }

        return session.ACA_EmpleadoId != employee.Id
            ? OperationResult<CashSessionResponse>.Forbidden("Solo quien abrió la caja puede registrar movimientos en ella.")
            : null;
    }

    private sealed record MethodTotal(int MethodId, string Code, string Name, bool AffectsCash, decimal Income, decimal Expense)
    {
        public decimal Net => Income - Expense;
    }

    private async Task<List<MethodTotal>> MethodTotalsAsync(int sessionId, CancellationToken cancellationToken)
    {
        var rows = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(item => item.MCA_AperturaCajaId == sessionId && !item.MCA_Anulado)
            .GroupBy(item => new
            {
                item.MCA_MetodoPagoId,
                item.MetodoPago.MPA_Codigo,
                item.MetodoPago.MPA_Nombre,
                item.MetodoPago.MPA_AfectaEfectivo,
                item.TipoMovimientoCaja.TMC_Naturaleza
            })
            .Select(group => new { group.Key, Amount = group.Sum(item => item.MCA_MontoBase) })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new { row.Key.MCA_MetodoPagoId, row.Key.MPA_Codigo, row.Key.MPA_Nombre, row.Key.MPA_AfectaEfectivo })
            .Select(group => new MethodTotal(
                group.Key.MCA_MetodoPagoId,
                group.Key.MPA_Codigo,
                group.Key.MPA_Nombre,
                group.Key.MPA_AfectaEfectivo,
                group.Where(row => row.Key.TMC_Naturaleza > 0).Sum(row => row.Amount),
                group.Where(row => row.Key.TMC_Naturaleza < 0).Sum(row => row.Amount)))
            .OrderBy(total => total.Name)
            .ToList();
    }

    private async Task<CashSessionResponse?> BuildResponseAsync(int sessionId, int? employeeId, CancellationToken cancellationToken)
    {
        CashSessionResponse? response = await _context.AperturasCaja
            .AsNoTracking()
            .Where(item => item.ACA_AperturaCaja == sessionId)
            .Select(item => new CashSessionResponse
            {
                Id = item.ACA_AperturaCaja,
                CashRegisterId = item.ACA_CajaId,
                CashRegister = item.Caja.CAJ_Nombre,
                EmployeeId = item.ACA_EmpleadoId,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                           " (" + item.Empleado.EMP_Codigo + ")",
                OpenedAt = item.ACA_FechaApertura,
                IsOpen = item.ACA_Abierta,
                Notes = item.ACA_Observaciones,
                InitialAmount = item.ACA_MontoInicial,
                Closing = item.CierreCaja == null ? null : new CashClosingResponse
                {
                    ClosedAt = item.CierreCaja.CCA_FechaCierre,
                    ClosedBy = item.CierreCaja.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                               item.CierreCaja.Empleado.Persona.PersonaNatural.PNA_PrimerApellido +
                               " (" + item.CierreCaja.Empleado.EMP_Codigo + ")",
                    ExpectedCash = item.CierreCaja.CCA_MontoEsperado,
                    CountedCash = item.CierreCaja.CCA_MontoContado,
                    Difference = item.CierreCaja.CCA_Diferencia,
                    Notes = item.CierreCaja.CCA_Observaciones,
                    Lines = item.CierreCaja.Detalles
                        .OrderBy(line => line.MetodoPago.MPA_Nombre)
                        .Select(line => new CashClosingLineResponse
                        {
                            PaymentMethod = line.MetodoPago.MPA_Nombre,
                            System = line.CCD_MontoSistema,
                            Counted = line.CCD_MontoContado,
                            Difference = line.CCD_Diferencia
                        })
                        .ToList()
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return null;
        }

        List<MethodTotal> totals = await MethodTotalsAsync(sessionId, cancellationToken);

        response.CashIncome = totals.Where(item => item.AffectsCash).Sum(item => item.Income);
        response.CashExpense = totals.Where(item => item.AffectsCash).Sum(item => item.Expense);
        response.ExpectedCash = response.InitialAmount + response.CashIncome - response.CashExpense;
        response.IsMine = response.IsOpen && response.EmployeeId == employeeId;
        response.Methods = totals
            .Select(item => new CashMethodSummaryResponse
            {
                PaymentMethodId = item.MethodId,
                Code = item.Code,
                Name = item.Name,
                AffectsCash = item.AffectsCash,
                Income = item.Income,
                Expense = item.Expense,
                Net = item.Net
            })
            .ToList();

        bool canAnnul = response.IsOpen;

        response.Movements = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(item => item.MCA_AperturaCajaId == sessionId)
            .OrderByDescending(item => item.MCA_Fecha)
            .ThenByDescending(item => item.MCA_MovimientoCaja)
            .Select(item => new CashMovementResponse
            {
                Id = item.MCA_MovimientoCaja,
                Number = item.MCA_Numero,
                Date = item.MCA_Fecha,
                TypeCode = item.TipoMovimientoCaja.TMC_Codigo,
                Type = item.TipoMovimientoCaja.TMC_Nombre,
                Nature = item.TipoMovimientoCaja.TMC_Naturaleza,
                PaymentMethod = item.MetodoPago.MPA_Nombre,
                Amount = item.MCA_MontoBase,
                Concept = item.MCA_Concepto,
                Reference = item.MCA_Referencia,
                SaleId = item.MCA_VentaId,
                SaleNumber = item.Venta != null ? item.Venta.VEN_Numero : null,
                Employee = item.Empleado.Persona.PersonaNatural!.PNA_PrimerNombre + " " +
                           item.Empleado.Persona.PersonaNatural.PNA_PrimerApellido,
                Annulled = item.MCA_Anulado,
                AnnulmentReason = item.MCA_MotivoAnulacion,
                CanAnnul = canAnnul && !item.MCA_Anulado && item.MCA_VentaId == null && item.MCA_DevolucionVentaId == null
            })
            .ToListAsync(cancellationToken);

        return response;
    }

    private async Task<int> LocalCurrencyIdAsync(CancellationToken cancellationToken) =>
        await _context.Monedas
            .Where(item => item.MON_Codigo == LocalCurrency)
            .Select(item => item.MON_Moneda)
            .SingleAsync(cancellationToken);

    private async Task<int> CashMethodIdAsync(CancellationToken cancellationToken) =>
        await _context.MetodosPago
            .Where(item => item.MPA_Codigo == PaymentMethodCode.Cash)
            .Select(item => item.MPA_MetodoPago)
            .SingleAsync(cancellationToken);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
