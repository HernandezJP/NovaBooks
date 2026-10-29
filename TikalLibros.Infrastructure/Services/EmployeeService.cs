using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Employees;
using TikalLibros.Application.Interfaces;
using TikalLibros.Application.Validation;
using TikalLibros.Domain.Entities.Administration;
using TikalLibros.Domain.Entities.People;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services;

public sealed class EmployeeService : IEmployeeService
{
    private const string CodePrefix = "EMP-";
    private const string DpiCode = "DPI";
    private const string NaturalPersonCode = "NATURAL";

    private readonly AppDbContext _context;
    private readonly PersonContactWriter _contacts;
    private readonly IUserService _users;

    public EmployeeService(
        AppDbContext context,
        PersonContactWriter contacts,
        IUserService users)
    {
        _context = context;
        _contacts = contacts;
        _users = users;
    }

    public async Task<PagedResponse<EmployeeListItemResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int? positionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => 10, > 100 => 100, _ => pageSize };

        int dpiTypeId = await GetDpiTypeIdAsync(cancellationToken);

        IQueryable<PB_EMPLEADO> query = _context.Empleados.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            query = query.Where(employee =>
                employee.EMP_Codigo.Contains(value) ||
                employee.Persona.PersonaNatural!.PNA_PrimerNombre.Contains(value) ||
                (employee.Persona.PersonaNatural.PNA_SegundoNombre != null &&
                 employee.Persona.PersonaNatural.PNA_SegundoNombre.Contains(value)) ||
                employee.Persona.PersonaNatural.PNA_PrimerApellido.Contains(value) ||
                (employee.Persona.PersonaNatural.PNA_SegundoApellido != null &&
                 employee.Persona.PersonaNatural.PNA_SegundoApellido.Contains(value)) ||
                employee.Persona.Identificaciones.Any(item =>
                    item.PID_Activo && item.PID_Numero.Contains(value)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(employee => employee.EMP_Activo == isActive.Value);
        }

        if (positionId.HasValue)
        {
            query = query.Where(employee => employee.EMP_PuestoId == positionId.Value);
        }

        int totalItems = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(employee => employee.Persona.PersonaNatural!.PNA_PrimerApellido)
            .ThenBy(employee => employee.Persona.PersonaNatural!.PNA_PrimerNombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(employee => new
            {
                employee.EMP_Empleado,
                employee.EMP_Codigo,
                employee.Persona.PersonaNatural!.PNA_PrimerNombre,
                employee.Persona.PersonaNatural.PNA_SegundoNombre,
                employee.Persona.PersonaNatural.PNA_PrimerApellido,
                employee.Persona.PersonaNatural.PNA_SegundoApellido,
                Dpi = employee.Persona.Identificaciones
                    .Where(item => item.PID_Activo && item.PID_TipoIdentificacionId == dpiTypeId)
                    .Select(item => item.PID_Numero)
                    .FirstOrDefault(),
                Email = employee.Persona.Correos
                    .Where(item => item.PCO_Activo && item.PCO_Principal)
                    .Select(item => item.PCO_Correo)
                    .FirstOrDefault(),
                Phone = employee.Persona.Telefonos
                    .Where(item => item.PTE_Activo && item.PTE_Principal)
                    .Select(item => item.PTE_Numero)
                    .FirstOrDefault(),
                Position = employee.Puesto.PUE_Nombre,
                Branch = employee.Sucursal.SUC_Nombre,
                employee.EMP_FechaContratacion,
                employee.EMP_Activo
            })
            .ToListAsync(cancellationToken);

        Dictionary<int, (int Id, string? UserName)> users =
            await LoadUsersAsync(rows.Select(row => row.EMP_Empleado).ToArray(), cancellationToken);

        return new PagedResponse<EmployeeListItemResponse>
        {
            Items = rows
                .Select(row => new EmployeeListItemResponse
                {
                    Id = row.EMP_Empleado,
                    Code = row.EMP_Codigo,
                    FullName = BuildFullName(row.PNA_PrimerNombre, row.PNA_SegundoNombre, row.PNA_PrimerApellido, row.PNA_SegundoApellido),
                    Dpi = row.Dpi,
                    Position = row.Position,
                    Branch = row.Branch,
                    Email = row.Email,
                    Phone = row.Phone,
                    HireDate = row.EMP_FechaContratacion,
                    IsActive = row.EMP_Activo,
                    UserId = users.TryGetValue(row.EMP_Empleado, out var user) ? user.Id : null,
                    UserName = users.TryGetValue(row.EMP_Empleado, out var named) ? named.UserName : null
                })
                .ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<EmployeeResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        int dpiTypeId = await GetDpiTypeIdAsync(cancellationToken);

        var row = await _context.Empleados
            .AsNoTracking()
            .Where(employee => employee.EMP_Empleado == id)
            .Select(employee => new
            {
                employee.EMP_Empleado,
                employee.EMP_Codigo,
                Natural = employee.Persona.PersonaNatural!,
                Dpi = employee.Persona.Identificaciones
                    .Where(item => item.PID_Activo && item.PID_TipoIdentificacionId == dpiTypeId)
                    .Select(item => item.PID_Numero)
                    .FirstOrDefault(),
                Email = employee.Persona.Correos
                    .Where(item => item.PCO_Activo && item.PCO_Principal)
                    .Select(item => item.PCO_Correo)
                    .FirstOrDefault(),
                Phone = employee.Persona.Telefonos
                    .Where(item => item.PTE_Activo && item.PTE_Principal)
                    .Select(item => new { item.PTE_Numero, item.PTE_TipoTelefonoId })
                    .FirstOrDefault(),
                employee.EMP_PuestoId,
                Position = employee.Puesto.PUE_Nombre,
                employee.EMP_SucursalId,
                Branch = employee.Sucursal.SUC_Nombre,
                employee.EMP_FechaContratacion,
                employee.EMP_FechaFinalizacion,
                employee.EMP_Activo,
                employee.EMP_FechaCreacion,
                employee.EMP_FechaModificacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(item => item.USU_EmpleadoId == id)
            .Select(item => new { item.Id, item.UserName, item.USU_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        return new EmployeeResponse
        {
            Id = row.EMP_Empleado,
            Code = row.EMP_Codigo,
            FullName = BuildFullName(row.Natural.PNA_PrimerNombre, row.Natural.PNA_SegundoNombre, row.Natural.PNA_PrimerApellido, row.Natural.PNA_SegundoApellido),
            FirstName = row.Natural.PNA_PrimerNombre,
            MiddleName = row.Natural.PNA_SegundoNombre,
            LastName = row.Natural.PNA_PrimerApellido,
            SecondLastName = row.Natural.PNA_SegundoApellido,
            BirthDate = row.Natural.PNA_FechaNacimiento,
            Dpi = row.Dpi,
            Email = row.Email,
            Phone = row.Phone?.PTE_Numero,
            PhoneTypeId = row.Phone?.PTE_TipoTelefonoId,
            PositionId = row.EMP_PuestoId,
            Position = row.Position,
            BranchId = row.EMP_SucursalId,
            Branch = row.Branch,
            HireDate = row.EMP_FechaContratacion,
            EndDate = row.EMP_FechaFinalizacion,
            IsActive = row.EMP_Activo,
            UserId = user?.Id,
            UserName = user?.UserName,
            UserIsActive = user?.USU_Activo,
            CreatedAt = row.EMP_FechaCreacion,
            ModifiedAt = row.EMP_FechaModificacion
        };
    }

    public async Task<EmployeeCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        return new EmployeeCatalogsResponse
        {
            Positions = await _context.Puestos
                .AsNoTracking()
                .Where(item => item.PUE_Activo)
                .OrderBy(item => item.PUE_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.PUE_Puesto, Code = string.Empty, Name = item.PUE_Nombre })
                .ToListAsync(cancellationToken),
            Branches = await _context.Sucursales
                .AsNoTracking()
                .Where(item => item.SUC_Activo)
                .OrderByDescending(item => item.SUC_EsPrincipal)
                .ThenBy(item => item.SUC_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.SUC_Sucursal, Code = item.SUC_Codigo, Name = item.SUC_Nombre })
                .ToListAsync(cancellationToken),
            PhoneTypes = await _context.TiposTelefono
                .AsNoTracking()
                .Where(item => item.TTE_Activo)
                .OrderBy(item => item.TTE_Nombre)
                .Select(item => new CatalogOptionResponse { Id = item.TTE_TipoTelefono, Code = item.TTE_Codigo, Name = item.TTE_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<OperationResult<EmployeeResponse>> CreateAsync(
        SaveEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        EmployeeInput input = EmployeeInput.From(request);
        int dpiTypeId = await GetDpiTypeIdAsync(cancellationToken);

        string[] errors = await ValidateReferencesAsync(input, cancellationToken);

        if (errors.Length > 0)
        {
            return OperationResult<EmployeeResponse>.Failure(errors);
        }

        string? conflict = await FindConflictAsync(input, dpiTypeId, excludingPersonId: null, cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<EmployeeResponse>.Conflict(conflict);
        }

        int naturalPersonType = await _context.TiposPersona
            .Where(type => type.TPR_Codigo == NaturalPersonCode)
            .Select(type => type.TPR_TipoPersona)
            .SingleAsync(cancellationToken);

        string code = await GenerateCodeAsync(cancellationToken);

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_EMPLEADO employee = new()
                    {
                        EMP_Codigo = code,
                        EMP_Activo = true,
                        EMP_FechaCreacion = DateTime.UtcNow,
                        Persona = new PB_PERSONA
                        {
                            PER_TipoPersonaId = naturalPersonType,
                            PER_Activo = true,
                            PER_FechaCreacion = DateTime.UtcNow,
                            PersonaNatural = new PB_PERSONA_NATURAL()
                        }
                    };

                    Apply(employee, input);
                    _context.Empleados.Add(employee);

                    await _context.SaveChangesAsync(token);
                    await _contacts.SyncAsync(employee.EMP_PersonaId, input.ToContacts(dpiTypeId), token);

                    return OperationResult<EmployeeResponse>.Success(
                        (await GetByIdAsync(employee.EMP_Empleado, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<EmployeeResponse>> UpdateAsync(
        int id,
        SaveEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_EMPLEADO? employee = await _context.Empleados
            .Include(item => item.Persona)
                .ThenInclude(person => person.PersonaNatural)
            .FirstOrDefaultAsync(item => item.EMP_Empleado == id, cancellationToken);

        if (employee is null)
        {
            return OperationResult<EmployeeResponse>.Missing("El empleado solicitado no existe.");
        }

        EmployeeInput input = EmployeeInput.From(request);
        int dpiTypeId = await GetDpiTypeIdAsync(cancellationToken);

        string[] errors = await ValidateReferencesAsync(input, cancellationToken);

        if (errors.Length > 0)
        {
            return OperationResult<EmployeeResponse>.Failure(errors);
        }

        string? conflict = await FindConflictAsync(input, dpiTypeId, employee.EMP_PersonaId, cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<EmployeeResponse>.Conflict(conflict);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    employee.EMP_FechaModificacion = DateTime.UtcNow;
                    employee.Persona.PER_FechaModificacion = DateTime.UtcNow;
                    employee.Persona.PersonaNatural ??= new PB_PERSONA_NATURAL();

                    Apply(employee, input);

                    await _context.SaveChangesAsync(token);
                    await _contacts.SyncAsync(employee.EMP_PersonaId, input.ToContacts(dpiTypeId), token);

                    return OperationResult<EmployeeResponse>.Success(
                        (await GetByIdAsync(employee.EMP_Empleado, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<EmployeeResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        PB_EMPLEADO? employee = await _context.Empleados
            .FirstOrDefaultAsync(item => item.EMP_Empleado == id, cancellationToken);

        if (employee is null)
        {
            return OperationResult<EmployeeResponse>.Missing("El empleado solicitado no existe.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(item => item.USU_EmpleadoId == id)
            .Select(item => new { item.Id, item.UserName, item.USU_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (!isActive && user?.Id == authenticatedUserId)
        {
            return OperationResult<EmployeeResponse>.Conflict(
                "No puede desactivar al empleado vinculado a su propio usuario.");
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                // Eliminación lógica: el empleado se conserva para el
                // historial de compras, recepciones y movimientos.
                employee.EMP_Activo = isActive;
                employee.EMP_FechaModificacion = DateTime.UtcNow;

                await _context.SaveChangesAsync(token);

                if (!isActive && user is { USU_Activo: true })
                {
                    OperationResult<Application.DTOs.Users.UserResponse> userResult =
                        await _users.ChangeStatusAsync(user.Id, false, authenticatedUserId, token);

                    if (!userResult.Succeeded)
                    {
                        return OperationResult<EmployeeResponse>.Conflict(
                            $"No se puede desactivar al empleado porque su usuario {user.UserName} " +
                            $"no puede desactivarse: {string.Join(" ", userResult.Errors)}");
                    }
                }

                return OperationResult<EmployeeResponse>.Success((await GetByIdAsync(id, token))!);
            },
            cancellationToken);
    }

    private async Task<Dictionary<int, (int Id, string? UserName)>> LoadUsersAsync(
        int[] employeeIds,
        CancellationToken cancellationToken)
    {
        return (await _context.Users
                .AsNoTracking()
                .Where(user => user.USU_EmpleadoId.HasValue && employeeIds.Contains(user.USU_EmpleadoId.Value))
                .Select(user => new { EmployeeId = user.USU_EmpleadoId!.Value, user.Id, user.UserName })
                .ToListAsync(cancellationToken))
            .ToDictionary(user => user.EmployeeId, user => (user.Id, user.UserName));
    }

    private Task<int> GetDpiTypeIdAsync(CancellationToken cancellationToken)
    {
        return _context.TiposIdentificacion
            .Where(type => type.TID_Codigo == DpiCode)
            .Select(type => type.TID_TipoIdentificacion)
            .SingleAsync(cancellationToken);
    }

    private async Task<string[]> ValidateReferencesAsync(
        EmployeeInput input,
        CancellationToken cancellationToken)
    {
        List<string> errors = [];

        if (!await _context.Puestos.AnyAsync(item => item.PUE_Puesto == input.PositionId && item.PUE_Activo, cancellationToken))
        {
            errors.Add("El puesto seleccionado no existe o está inactivo.");
        }

        if (!await _context.Sucursales.AnyAsync(item => item.SUC_Sucursal == input.BranchId && item.SUC_Activo, cancellationToken))
        {
            errors.Add("La sucursal seleccionada no existe o está inactiva.");
        }

        if (input.Phone is not null &&
            !await _context.TiposTelefono.AnyAsync(item => item.TTE_TipoTelefono == input.PhoneTypeId && item.TTE_Activo, cancellationToken))
        {
            errors.Add("El tipo de teléfono no existe o está inactivo.");
        }

        return errors.ToArray();
    }

    /// <summary>DPI único entre todas las personas y correo único entre empleados.</summary>
    private async Task<string?> FindConflictAsync(
        EmployeeInput input,
        int dpiTypeId,
        int? excludingPersonId,
        CancellationToken cancellationToken)
    {
        string? owner = await _contacts.FindIdentificationOwnerAsync(
            dpiTypeId, input.Dpi, excludingPersonId, cancellationToken);

        if (owner is not null)
        {
            return $"El DPI {input.Dpi} ya está registrado{owner}.";
        }

        if (input.Email is not null)
        {
            string? emailOwner = await _context.PersonasCorreos
                .AsNoTracking()
                .Where(item =>
                    item.PCO_Activo &&
                    item.PCO_Correo == input.Email &&
                    item.PCO_PersonaId != excludingPersonId &&
                    item.Persona.Empleado != null)
                .Select(item => item.Persona.Empleado!.EMP_Codigo)
                .FirstOrDefaultAsync(cancellationToken);

            if (emailOwner is not null)
            {
                return $"El correo {input.Email} ya está registrado para el empleado {emailOwner}.";
            }
        }

        return null;
    }

    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        int next = (await _context.Empleados.MaxAsync(item => (int?)item.EMP_Empleado, cancellationToken) ?? 0) + 1;

        while (true)
        {
            string code = $"{CodePrefix}{next:D4}";

            if (!await _context.Empleados.AnyAsync(item => item.EMP_Codigo == code, cancellationToken))
            {
                return code;
            }

            next++;
        }
    }

    private static void Apply(PB_EMPLEADO employee, EmployeeInput input)
    {
        PB_PERSONA_NATURAL natural = employee.Persona.PersonaNatural!;
        natural.PNA_PrimerNombre = input.FirstName;
        natural.PNA_SegundoNombre = input.MiddleName;
        natural.PNA_PrimerApellido = input.LastName;
        natural.PNA_SegundoApellido = input.SecondLastName;
        natural.PNA_FechaNacimiento = input.BirthDate;

        employee.EMP_PuestoId = input.PositionId;
        employee.EMP_SucursalId = input.BranchId;
        employee.EMP_FechaContratacion = input.HireDate;
        employee.EMP_FechaFinalizacion = input.EndDate;
    }

    private static string BuildFullName(string first, string? middle, string last, string? secondLast)
    {
        return string.Join(" ", new[] { first, middle, last, secondLast }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static OperationResult<EmployeeResponse> ConcurrentConflict()
    {
        return OperationResult<EmployeeResponse>.Conflict(
            "Otro empleado con el mismo código o DPI se guardó al mismo tiempo. " +
            "Revise los datos e intente nuevamente.");
    }

    /// <summary>Valores del request ya normalizados; los vacíos quedan en null.</summary>
    private sealed record EmployeeInput(
        string FirstName,
        string? MiddleName,
        string LastName,
        string? SecondLastName,
        DateOnly? BirthDate,
        string Dpi,
        string? Email,
        int? PhoneTypeId,
        string? Phone,
        int PositionId,
        int BranchId,
        DateOnly HireDate,
        DateOnly? EndDate)
    {
        public static EmployeeInput From(SaveEmployeeRequest request)
        {
            string? phone = Clean(request.Phone);

            return new EmployeeInput(
                CleanName(request.FirstName)!,
                CleanName(request.MiddleName),
                CleanName(request.LastName)!,
                CleanName(request.SecondLastName),
                request.BirthDate,
                DpiRules.Normalize(request.Dpi),
                Clean(request.Email) is { } email ? ContactRules.NormalizeEmail(email) : null,
                phone is null ? null : request.PhoneTypeId,
                phone is null ? null : ContactRules.NormalizePhone(phone),
                request.PositionId!.Value,
                request.BranchId!.Value,
                request.HireDate!.Value,
                request.EndDate);
        }

        public PersonContacts ToContacts(int dpiTypeId) =>
            new([new PersonIdentification(dpiTypeId, Dpi, Principal: true)], Email, PhoneTypeId, Phone, null);

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>Recorta y colapsa espacios repetidos ("Ana   María").</summary>
        private static string? CleanName(string? value) =>
            Clean(value) is { } text ? string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) : null;
    }
}
