using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Application.Interfaces;
using TikalLibros.Application.Validation;
using TikalLibros.Domain.Entities.People;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Services.People;

namespace TikalLibros.Infrastructure.Services;

public sealed class SupplierService : ISupplierService
{
    private const string CodePrefix = "PRO-";
    private const string NitCode = "NIT";

    /// <summary>Estados de orden que impiden desactivar al proveedor.</summary>
    internal static readonly string[] OpenOrderStates = ["BORRADOR", "EMITIDA", "PARCIAL"];

    private readonly AppDbContext _context;
    private readonly PersonContactWriter _contacts;

    public SupplierService(AppDbContext context, PersonContactWriter contacts)
    {
        _context = context;
        _contacts = contacts;
    }

    public async Task<PagedResponse<SupplierListItemResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        string? personType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => 10, > 100 => 100, _ => pageSize };

        IQueryable<PB_PROVEEDOR> suppliers = _context.Proveedores.AsNoTracking();

        if (isActive.HasValue)
        {
            suppliers = suppliers.Where(supplier => supplier.PRO_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(personType))
        {
            string type = personType.Trim().ToUpperInvariant();
            suppliers = suppliers.Where(supplier => supplier.Persona.TipoPersona.TPR_Codigo == type);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();
            string identification = ContactRules.NormalizeIdentification(value);

            suppliers = suppliers.Where(supplier =>
                supplier.PRO_Codigo.Contains(value) ||
                (supplier.Persona.PersonaJuridica != null &&
                 (supplier.Persona.PersonaJuridica.PJU_RazonSocial.Contains(value) ||
                  (supplier.Persona.PersonaJuridica.PJU_NombreComercial != null &&
                   supplier.Persona.PersonaJuridica.PJU_NombreComercial.Contains(value)))) ||
                (supplier.Persona.PersonaNatural != null &&
                 (supplier.Persona.PersonaNatural.PNA_PrimerNombre.Contains(value) ||
                  supplier.Persona.PersonaNatural.PNA_PrimerApellido.Contains(value) ||
                  (supplier.Persona.PersonaNatural.PNA_PrimerNombre + " " +
                   supplier.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value))) ||
                supplier.Persona.Identificaciones.Any(item =>
                    item.PID_Activo && item.PID_Numero.Contains(identification)) ||
                supplier.Persona.Correos.Any(item =>
                    item.PCO_Activo && item.PCO_Correo.Contains(value)));
        }

        int totalItems = await suppliers.CountAsync(cancellationToken);

        var rows = await suppliers
            .OrderBy(supplier => supplier.Persona.PersonaJuridica != null
                ? supplier.Persona.PersonaJuridica.PJU_RazonSocial
                : supplier.Persona.PersonaNatural!.PNA_PrimerNombre)
            .ThenBy(supplier => supplier.PRO_Codigo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(supplier => new
            {
                supplier.PRO_Proveedor,
                supplier.PRO_Codigo,
                PersonType = supplier.Persona.TipoPersona.TPR_Codigo,
                FirstName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_PrimerNombre : null,
                MiddleName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_SegundoNombre : null,
                LastName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_PrimerApellido : null,
                SecondLastName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_SegundoApellido : null,
                LegalName = supplier.Persona.PersonaJuridica != null ? supplier.Persona.PersonaJuridica.PJU_RazonSocial : null,
                TradeName = supplier.Persona.PersonaJuridica != null ? supplier.Persona.PersonaJuridica.PJU_NombreComercial : null,
                Nit = supplier.Persona.Identificaciones
                    .Where(item => item.PID_Activo && item.TipoIdentificacion.TID_Codigo == NitCode)
                    .Select(item => item.PID_Numero)
                    .FirstOrDefault(),
                Email = supplier.Persona.Correos
                    .Where(item => item.PCO_Activo && item.PCO_Principal)
                    .Select(item => item.PCO_Correo)
                    .FirstOrDefault(),
                Phone = supplier.Persona.Telefonos
                    .Where(item => item.PTE_Activo && item.PTE_Principal)
                    .Select(item => item.PTE_Numero)
                    .FirstOrDefault(),
                supplier.PRO_LimiteCredito,
                supplier.PRO_DiasCredito,
                OpenOrders = _context.OrdenesCompra.Count(order =>
                    order.OCO_ProveedorId == supplier.PRO_Proveedor &&
                    order.OCO_Activo &&
                    OpenOrderStates.Contains(order.EstadoOrdenCompra.EOC_Codigo)),
                supplier.PRO_Activo
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<SupplierListItemResponse>
        {
            Items = rows
                .Select(row => new SupplierListItemResponse
                {
                    Id = row.PRO_Proveedor,
                    Code = row.PRO_Codigo,
                    PersonType = row.PersonType,
                    DisplayName = BuildDisplayName(row.FirstName, row.MiddleName, row.LastName, row.SecondLastName, row.LegalName),
                    TradeName = row.TradeName,
                    Nit = row.Nit,
                    Email = row.Email,
                    Phone = row.Phone,
                    CreditLimit = row.PRO_LimiteCredito,
                    CreditDays = row.PRO_DiasCredito,
                    OpenOrders = row.OpenOrders,
                    IsActive = row.PRO_Activo
                })
                .ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<SupplierResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        SupplierResponse? response = await _context.Proveedores
            .AsNoTracking()
            .Where(supplier => supplier.PRO_Proveedor == id)
            .Select(supplier => new SupplierResponse
            {
                Id = supplier.PRO_Proveedor,
                Code = supplier.PRO_Codigo,
                PersonType = supplier.Persona.TipoPersona.TPR_Codigo,
                FirstName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_PrimerNombre : null,
                MiddleName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_SegundoNombre : null,
                LastName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_PrimerApellido : null,
                SecondLastName = supplier.Persona.PersonaNatural != null ? supplier.Persona.PersonaNatural.PNA_SegundoApellido : null,
                LegalName = supplier.Persona.PersonaJuridica != null ? supplier.Persona.PersonaJuridica.PJU_RazonSocial : null,
                TradeName = supplier.Persona.PersonaJuridica != null ? supplier.Persona.PersonaJuridica.PJU_NombreComercial : null,
                Nit = supplier.Persona.Identificaciones
                    .Where(item => item.PID_Activo && item.TipoIdentificacion.TID_Codigo == NitCode)
                    .Select(item => item.PID_Numero)
                    .FirstOrDefault(),
                Email = supplier.Persona.Correos
                    .Where(item => item.PCO_Activo && item.PCO_Principal)
                    .Select(item => item.PCO_Correo)
                    .FirstOrDefault(),
                PhoneTypeId = supplier.Persona.Telefonos
                    .Where(item => item.PTE_Activo && item.PTE_Principal)
                    .Select(item => (int?)item.PTE_TipoTelefonoId)
                    .FirstOrDefault(),
                Phone = supplier.Persona.Telefonos
                    .Where(item => item.PTE_Activo && item.PTE_Principal)
                    .Select(item => item.PTE_Numero)
                    .FirstOrDefault(),
                PhoneExtension = supplier.Persona.Telefonos
                    .Where(item => item.PTE_Activo && item.PTE_Principal)
                    .Select(item => item.PTE_Extension)
                    .FirstOrDefault(),
                CreditLimit = supplier.PRO_LimiteCredito,
                CreditDays = supplier.PRO_DiasCredito,
                Notes = supplier.PRO_Observaciones,
                OpenOrders = _context.OrdenesCompra.Count(order =>
                    order.OCO_ProveedorId == supplier.PRO_Proveedor &&
                    order.OCO_Activo &&
                    OpenOrderStates.Contains(order.EstadoOrdenCompra.EOC_Codigo)),
                IsActive = supplier.PRO_Activo,
                CreatedAt = supplier.PRO_FechaCreacion,
                ModifiedAt = supplier.PRO_FechaModificacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
        {
            response.DisplayName = BuildDisplayName(
                response.FirstName, response.MiddleName, response.LastName, response.SecondLastName, response.LegalName);
        }

        return response;
    }

    public async Task<SupplierCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        return new SupplierCatalogsResponse
        {
            PersonTypes = await _context.TiposPersona
                .AsNoTracking()
                .Where(type => type.TPR_Activo)
                .OrderBy(type => type.TPR_Nombre)
                .Select(type => new CatalogOptionResponse { Id = type.TPR_TipoPersona, Code = type.TPR_Codigo, Name = type.TPR_Nombre })
                .ToListAsync(cancellationToken),
            PhoneTypes = await _context.TiposTelefono
                .AsNoTracking()
                .Where(type => type.TTE_Activo)
                .OrderBy(type => type.TTE_Nombre)
                .Select(type => new CatalogOptionResponse { Id = type.TTE_TipoTelefono, Code = type.TTE_Codigo, Name = type.TTE_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<OperationResult<SupplierResponse>> CreateAsync(
        SaveSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        SupplierInput input = SupplierInput.From(request);
        References references = await LoadReferencesAsync(cancellationToken);

        string[] errors = Validate(input, references);

        if (errors.Length > 0)
        {
            return OperationResult<SupplierResponse>.Failure(errors);
        }

        string? conflict = await FindConflictAsync(input, references, excludingPersonId: null, cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<SupplierResponse>.Conflict(conflict);
        }

        string code = await GenerateCodeAsync(cancellationToken);

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    PB_PROVEEDOR supplier = new()
                    {
                        PRO_Codigo = code,
                        PRO_Activo = true,
                        PRO_FechaCreacion = DateTime.UtcNow,
                        Persona = new PB_PERSONA
                        {
                            PER_TipoPersonaId = references.PersonTypeIds[input.PersonType],
                            PER_Activo = true,
                            PER_FechaCreacion = DateTime.UtcNow
                        }
                    };

                    Apply(supplier, input);
                    _context.Proveedores.Add(supplier);

                    await _context.SaveChangesAsync(token);
                    await _contacts.SyncAsync(supplier.PRO_PersonaId, input.ToContacts(references.NitTypeId), token);

                    return OperationResult<SupplierResponse>.Success((await GetByIdAsync(supplier.PRO_Proveedor, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<SupplierResponse>> UpdateAsync(
        int id,
        SaveSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_PROVEEDOR? supplier = await _context.Proveedores
            .Include(item => item.Persona).ThenInclude(person => person.TipoPersona)
            .Include(item => item.Persona).ThenInclude(person => person.PersonaNatural)
            .Include(item => item.Persona).ThenInclude(person => person.PersonaJuridica)
            .FirstOrDefaultAsync(item => item.PRO_Proveedor == id, cancellationToken);

        if (supplier is null)
        {
            return OperationResult<SupplierResponse>.Missing("El proveedor solicitado no existe.");
        }

        SupplierInput input = SupplierInput.From(request);

        if (!string.Equals(supplier.Persona.TipoPersona.TPR_Codigo, input.PersonType, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<SupplierResponse>.Conflict(
                "No se puede cambiar el tipo de persona de un proveedor existente.");
        }

        References references = await LoadReferencesAsync(cancellationToken);
        string[] errors = Validate(input, references);

        if (errors.Length > 0)
        {
            return OperationResult<SupplierResponse>.Failure(errors);
        }

        string? conflict = await FindConflictAsync(input, references, supplier.PRO_PersonaId, cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<SupplierResponse>.Conflict(conflict);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    supplier.PRO_FechaModificacion = DateTime.UtcNow;
                    supplier.Persona.PER_FechaModificacion = DateTime.UtcNow;

                    Apply(supplier, input);

                    await _context.SaveChangesAsync(token);
                    await _contacts.SyncAsync(supplier.PRO_PersonaId, input.ToContacts(references.NitTypeId), token);

                    return OperationResult<SupplierResponse>.Success((await GetByIdAsync(supplier.PRO_Proveedor, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<SupplierResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_PROVEEDOR? supplier = await _context.Proveedores
            .FirstOrDefaultAsync(item => item.PRO_Proveedor == id, cancellationToken);

        if (supplier is null)
        {
            return OperationResult<SupplierResponse>.Missing("El proveedor solicitado no existe.");
        }

        if (!isActive)
        {
            int openOrders = await _context.OrdenesCompra.CountAsync(
                order => order.OCO_ProveedorId == id &&
                         order.OCO_Activo &&
                         OpenOrderStates.Contains(order.EstadoOrdenCompra.EOC_Codigo),
                cancellationToken);

            if (openOrders > 0)
            {
                return OperationResult<SupplierResponse>.Conflict(
                    $"No se puede desactivar el proveedor porque tiene {openOrders} orden(es) de compra abiertas. " +
                    "Recíbalas o cancélelas primero.");
            }
        }

        // Eliminación lógica: se conserva para el historial de compras.
        supplier.PRO_Activo = isActive;
        supplier.PRO_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<SupplierResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    private async Task<References> LoadReferencesAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, int> personTypes = await _context.TiposPersona
            .AsNoTracking()
            .Where(type => type.TPR_Activo)
            .ToDictionaryAsync(type => type.TPR_Codigo, type => type.TPR_TipoPersona, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var nitType = await _context.TiposIdentificacion
            .AsNoTracking()
            .Where(type => type.TID_Codigo == NitCode)
            .Select(type => new { type.TID_TipoIdentificacion, type.TID_LongitudMinima, type.TID_LongitudMaxima })
            .SingleAsync(cancellationToken);

        HashSet<int> phoneTypes = (await _context.TiposTelefono
                .AsNoTracking()
                .Where(type => type.TTE_Activo)
                .Select(type => type.TTE_TipoTelefono)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return new References(personTypes, nitType.TID_TipoIdentificacion, nitType.TID_LongitudMinima, nitType.TID_LongitudMaxima, phoneTypes);
    }

    private static string[] Validate(SupplierInput input, References references)
    {
        List<string> errors = [];

        if (!references.PersonTypeIds.ContainsKey(input.PersonType))
        {
            errors.Add("El tipo de persona no existe o está inactivo.");
        }

        if ((references.NitMinLength.HasValue && input.Nit.Length < references.NitMinLength) ||
            (references.NitMaxLength.HasValue && input.Nit.Length > references.NitMaxLength))
        {
            errors.Add($"El NIT debe tener entre {references.NitMinLength} y {references.NitMaxLength} caracteres.");
        }

        if (input.Phone is not null && !references.PhoneTypeIds.Contains(input.PhoneTypeId!.Value))
        {
            errors.Add("El tipo de teléfono no existe o está inactivo.");
        }

        return errors.ToArray();
    }

    /// <summary>NIT único entre todas las personas y correo único entre proveedores.</summary>
    private async Task<string?> FindConflictAsync(
        SupplierInput input,
        References references,
        int? excludingPersonId,
        CancellationToken cancellationToken)
    {
        string? owner = await _contacts.FindIdentificationOwnerAsync(
            references.NitTypeId, input.Nit, excludingPersonId, cancellationToken);

        if (owner is not null)
        {
            return $"El NIT {input.Nit} ya está registrado{owner}.";
        }

        if (input.Email is not null)
        {
            string? emailOwner = await _context.PersonasCorreos
                .AsNoTracking()
                .Where(item =>
                    item.PCO_Activo &&
                    item.PCO_Correo == input.Email &&
                    item.PCO_PersonaId != excludingPersonId &&
                    item.Persona.Proveedor != null)
                .Select(item => item.Persona.Proveedor!.PRO_Codigo)
                .FirstOrDefaultAsync(cancellationToken);

            if (emailOwner is not null)
            {
                return $"El correo {input.Email} ya está registrado para el proveedor {emailOwner}.";
            }
        }

        return null;
    }

    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        int next = (await _context.Proveedores.MaxAsync(item => (int?)item.PRO_Proveedor, cancellationToken) ?? 0) + 1;

        while (true)
        {
            string code = $"{CodePrefix}{next:D4}";

            if (!await _context.Proveedores.AnyAsync(item => item.PRO_Codigo == code, cancellationToken))
            {
                return code;
            }

            next++;
        }
    }

    private static void Apply(PB_PROVEEDOR supplier, SupplierInput input)
    {
        PB_PERSONA person = supplier.Persona;

        if (input.PersonType == SaveSupplierRequest.NaturalPerson)
        {
            person.PersonaNatural ??= new PB_PERSONA_NATURAL();
            person.PersonaNatural.PNA_PrimerNombre = input.FirstName!;
            person.PersonaNatural.PNA_SegundoNombre = input.MiddleName;
            person.PersonaNatural.PNA_PrimerApellido = input.LastName!;
            person.PersonaNatural.PNA_SegundoApellido = input.SecondLastName;
        }
        else
        {
            person.PersonaJuridica ??= new PB_PERSONA_JURIDICA();
            person.PersonaJuridica.PJU_RazonSocial = input.LegalName!;
            person.PersonaJuridica.PJU_NombreComercial = input.TradeName;
        }

        supplier.PRO_LimiteCredito = input.CreditLimit;
        supplier.PRO_DiasCredito = input.CreditDays;
        supplier.PRO_Observaciones = input.Notes;
    }

    private static string BuildDisplayName(string? firstName, string? middleName, string? lastName, string? secondLastName, string? legalName)
    {
        return !string.IsNullOrWhiteSpace(legalName)
            ? legalName
            : string.Join(" ", new[] { firstName, middleName, lastName, secondLastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static OperationResult<SupplierResponse> ConcurrentConflict()
    {
        return OperationResult<SupplierResponse>.Conflict(
            "Otro proveedor con el mismo código o NIT se guardó al mismo tiempo. Revise los datos e intente nuevamente.");
    }

    private sealed record References(
        Dictionary<string, int> PersonTypeIds,
        int NitTypeId,
        int? NitMinLength,
        int? NitMaxLength,
        HashSet<int> PhoneTypeIds);

    /// <summary>Valores del request ya normalizados; los vacíos quedan en null.</summary>
    private sealed record SupplierInput(
        string PersonType,
        string? FirstName,
        string? MiddleName,
        string? LastName,
        string? SecondLastName,
        string? LegalName,
        string? TradeName,
        string Nit,
        string? Email,
        int? PhoneTypeId,
        string? Phone,
        string? PhoneExtension,
        decimal CreditLimit,
        int CreditDays,
        string? Notes)
    {
        public static SupplierInput From(SaveSupplierRequest request)
        {
            string personType = request.PersonType.Trim().ToUpperInvariant();
            bool natural = personType == SaveSupplierRequest.NaturalPerson;
            string? phone = Clean(request.Phone);

            return new SupplierInput(
                personType,
                natural ? CleanName(request.FirstName) : null,
                natural ? CleanName(request.MiddleName) : null,
                natural ? CleanName(request.LastName) : null,
                natural ? CleanName(request.SecondLastName) : null,
                natural ? null : CleanName(request.LegalName),
                natural ? null : CleanName(request.TradeName),
                ContactRules.NormalizeIdentification(request.Nit),
                Clean(request.Email) is { } email ? ContactRules.NormalizeEmail(email) : null,
                phone is null ? null : request.PhoneTypeId,
                phone is null ? null : ContactRules.NormalizePhone(phone),
                phone is null ? null : Clean(request.PhoneExtension),
                request.CreditLimit,
                request.CreditDays,
                Clean(request.Notes));
        }

        public PersonContacts ToContacts(int nitTypeId) =>
            new([new PersonIdentification(nitTypeId, Nit, Principal: true)], Email, PhoneTypeId, Phone, PhoneExtension);

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>Recorta y colapsa espacios repetidos.</summary>
        private static string? CleanName(string? value) =>
            Clean(value) is { } text ? string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) : null;
    }
}
