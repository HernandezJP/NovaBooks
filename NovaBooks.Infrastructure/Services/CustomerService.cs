using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Common;
using NovaBooks.Application.DTOs.Customers;
using NovaBooks.Application.Interfaces;
using NovaBooks.Application.Validation;
using NovaBooks.Domain.Entities.People;
using NovaBooks.Infrastructure.Data;

namespace NovaBooks.Infrastructure.Services;

public sealed class CustomerService : ICustomerService
{
    private const string NitCode = "NIT";
    private const string DpiCode = "DPI";
    private const string CodePrefix = "CLI-";

    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<CustomerListItemResponse>> GetPagedAsync(
        CustomerQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = query.Page < 1 ? 1 : query.Page;

        int pageSize = query.PageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => query.PageSize
        };

        IQueryable<PB_CLIENTE> customers =
            _context.Clientes.AsNoTracking();

        if (query.IsActive.HasValue)
        {
            customers = customers.Where(customer =>
                customer.CLI_Activo == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PersonType))
        {
            string personType = query.PersonType.Trim().ToUpperInvariant();

            customers = customers.Where(customer =>
                customer.Persona.TipoPersona.TPR_Codigo == personType);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string value = query.Search.Trim();
            string identification = ContactRules.NormalizeIdentification(value);
            string phone = ContactRules.NormalizePhone(value);

            customers = customers.Where(customer =>
                customer.CLI_Codigo.Contains(value) ||
                (customer.Persona.PersonaNatural != null &&
                 (customer.Persona.PersonaNatural.PNA_PrimerNombre.Contains(value) ||
                  customer.Persona.PersonaNatural.PNA_PrimerApellido.Contains(value) ||
                  (customer.Persona.PersonaNatural.PNA_SegundoNombre != null &&
                   customer.Persona.PersonaNatural.PNA_SegundoNombre.Contains(value)) ||
                  (customer.Persona.PersonaNatural.PNA_SegundoApellido != null &&
                   customer.Persona.PersonaNatural.PNA_SegundoApellido.Contains(value)) ||
                  (customer.Persona.PersonaNatural.PNA_PrimerNombre + " " +
                   customer.Persona.PersonaNatural.PNA_PrimerApellido).Contains(value))) ||
                (customer.Persona.PersonaJuridica != null &&
                 (customer.Persona.PersonaJuridica.PJU_RazonSocial.Contains(value) ||
                  (customer.Persona.PersonaJuridica.PJU_NombreComercial != null &&
                   customer.Persona.PersonaJuridica.PJU_NombreComercial.Contains(value)))) ||
                customer.Persona.Identificaciones.Any(item =>
                    item.PID_Activo &&
                    item.PID_Numero.Contains(identification)) ||
                customer.Persona.Correos.Any(item =>
                    item.PCO_Activo &&
                    item.PCO_Correo.Contains(value)) ||
                (phone.Length >= 3 &&
                 customer.Persona.Telefonos.Any(item =>
                     item.PTE_Activo &&
                     item.PTE_Numero.Contains(phone))));
        }

        int totalItems = await customers.CountAsync(cancellationToken);

        var rows =
            await customers
                .OrderBy(customer =>
                    customer.Persona.PersonaJuridica != null
                        ? customer.Persona.PersonaJuridica.PJU_RazonSocial
                        : customer.Persona.PersonaNatural!.PNA_PrimerNombre)
                .ThenBy(customer => customer.CLI_Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(customer => new
                {
                    customer.CLI_Cliente,
                    customer.CLI_Codigo,
                    PersonType = customer.Persona.TipoPersona.TPR_Codigo,
                    FirstName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_PrimerNombre
                        : null,
                    MiddleName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_SegundoNombre
                        : null,
                    LastName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_PrimerApellido
                        : null,
                    SecondLastName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_SegundoApellido
                        : null,
                    LegalName = customer.Persona.PersonaJuridica != null
                        ? customer.Persona.PersonaJuridica.PJU_RazonSocial
                        : null,
                    Identification = customer.Persona.Identificaciones
                        .Where(item =>
                            item.PID_Activo &&
                            item.TipoIdentificacion.TID_Codigo != NitCode)
                        .Select(item =>
                            item.TipoIdentificacion.TID_Codigo + " " + item.PID_Numero)
                        .FirstOrDefault(),
                    Nit = customer.Persona.Identificaciones
                        .Where(item =>
                            item.PID_Activo &&
                            item.TipoIdentificacion.TID_Codigo == NitCode)
                        .Select(item => item.PID_Numero)
                        .FirstOrDefault(),
                    Email = customer.Persona.Correos
                        .Where(item => item.PCO_Activo)
                        .OrderByDescending(item => item.PCO_Principal)
                        .Select(item => item.PCO_Correo)
                        .FirstOrDefault(),
                    Phone = customer.Persona.Telefonos
                        .Where(item => item.PTE_Activo)
                        .OrderByDescending(item => item.PTE_Principal)
                        .Select(item => item.PTE_Numero)
                        .FirstOrDefault(),
                    customer.CLI_LimiteCredito,
                    customer.CLI_DiasCredito,
                    customer.CLI_Activo
                })
                .ToListAsync(cancellationToken);

        return new PagedResponse<CustomerListItemResponse>
        {
            Items = rows
                .Select(row => new CustomerListItemResponse
                {
                    Id = row.CLI_Cliente,
                    Code = row.CLI_Codigo,
                    PersonType = row.PersonType,
                    DisplayName = BuildDisplayName(
                        row.FirstName,
                        row.MiddleName,
                        row.LastName,
                        row.SecondLastName,
                        row.LegalName),
                    Identification = row.Identification,
                    Nit = row.Nit,
                    Email = row.Email,
                    Phone = row.Phone,
                    CreditLimit = row.CLI_LimiteCredito,
                    CreditDays = row.CLI_DiasCredito,
                    IsActive = row.CLI_Activo
                })
                .ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<CustomerResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CustomerResponse? response =
            await _context.Clientes
                .AsNoTracking()
                .Where(customer => customer.CLI_Cliente == id)
                .Select(customer => new CustomerResponse
                {
                    Id = customer.CLI_Cliente,
                    Code = customer.CLI_Codigo,
                    PersonType = customer.Persona.TipoPersona.TPR_Codigo,
                    FirstName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_PrimerNombre
                        : null,
                    MiddleName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_SegundoNombre
                        : null,
                    LastName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_PrimerApellido
                        : null,
                    SecondLastName = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_SegundoApellido
                        : null,
                    BirthDate = customer.Persona.PersonaNatural != null
                        ? customer.Persona.PersonaNatural.PNA_FechaNacimiento
                        : null,
                    LegalName = customer.Persona.PersonaJuridica != null
                        ? customer.Persona.PersonaJuridica.PJU_RazonSocial
                        : null,
                    TradeName = customer.Persona.PersonaJuridica != null
                        ? customer.Persona.PersonaJuridica.PJU_NombreComercial
                        : null,
                    IdentificationTypeId = customer.Persona.Identificaciones
                        .Where(item =>
                            item.PID_Activo &&
                            item.TipoIdentificacion.TID_Codigo != NitCode)
                        .Select(item => (int?)item.PID_TipoIdentificacionId)
                        .FirstOrDefault(),
                    IdentificationTypeCode = customer.Persona.Identificaciones
                        .Where(item =>
                            item.PID_Activo &&
                            item.TipoIdentificacion.TID_Codigo != NitCode)
                        .Select(item => item.TipoIdentificacion.TID_Codigo)
                        .FirstOrDefault(),
                    IdentificationNumber = customer.Persona.Identificaciones
                        .Where(item =>
                            item.PID_Activo &&
                            item.TipoIdentificacion.TID_Codigo != NitCode)
                        .Select(item => item.PID_Numero)
                        .FirstOrDefault(),
                    Nit = customer.Persona.Identificaciones
                        .Where(item =>
                            item.PID_Activo &&
                            item.TipoIdentificacion.TID_Codigo == NitCode)
                        .Select(item => item.PID_Numero)
                        .FirstOrDefault(),
                    Email = customer.Persona.Correos
                        .Where(item => item.PCO_Activo)
                        .OrderByDescending(item => item.PCO_Principal)
                        .Select(item => item.PCO_Correo)
                        .FirstOrDefault(),
                    PhoneTypeId = customer.Persona.Telefonos
                        .Where(item => item.PTE_Activo)
                        .OrderByDescending(item => item.PTE_Principal)
                        .Select(item => (int?)item.PTE_TipoTelefonoId)
                        .FirstOrDefault(),
                    Phone = customer.Persona.Telefonos
                        .Where(item => item.PTE_Activo)
                        .OrderByDescending(item => item.PTE_Principal)
                        .Select(item => item.PTE_Numero)
                        .FirstOrDefault(),
                    PhoneExtension = customer.Persona.Telefonos
                        .Where(item => item.PTE_Activo)
                        .OrderByDescending(item => item.PTE_Principal)
                        .Select(item => item.PTE_Extension)
                        .FirstOrDefault(),
                    CreditLimit = customer.CLI_LimiteCredito,
                    CreditDays = customer.CLI_DiasCredito,
                    Notes = customer.CLI_Observaciones,
                    IsActive = customer.CLI_Activo,
                    CreatedAt = customer.CLI_FechaCreacion,
                    ModifiedAt = customer.CLI_FechaModificacion
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
        {
            response.DisplayName = BuildDisplayName(
                response.FirstName,
                response.MiddleName,
                response.LastName,
                response.SecondLastName,
                response.LegalName);
        }

        return response;
    }

    public async Task<CustomerCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        return new CustomerCatalogsResponse
        {
            PersonTypes =
                await _context.TiposPersona
                    .AsNoTracking()
                    .Where(type => type.TPR_Activo)
                    .OrderBy(type => type.TPR_Nombre)
                    .Select(type => new CatalogOptionResponse
                    {
                        Id = type.TPR_TipoPersona,
                        Code = type.TPR_Codigo,
                        Name = type.TPR_Nombre
                    })
                    .ToListAsync(cancellationToken),

            IdentificationTypes =
                await _context.TiposIdentificacion
                    .AsNoTracking()
                    .Where(type => type.TID_Activo && type.TID_Codigo != NitCode)
                    .OrderBy(type => type.TID_Nombre)
                    .Select(type => new IdentificationTypeResponse
                    {
                        Id = type.TID_TipoIdentificacion,
                        Code = type.TID_Codigo,
                        Name = type.TID_Nombre,
                        MinLength = type.TID_LongitudMinima,
                        MaxLength = type.TID_LongitudMaxima
                    })
                    .ToListAsync(cancellationToken),

            PhoneTypes =
                await _context.TiposTelefono
                    .AsNoTracking()
                    .Where(type => type.TTE_Activo)
                    .OrderBy(type => type.TTE_Nombre)
                    .Select(type => new CatalogOptionResponse
                    {
                        Id = type.TTE_TipoTelefono,
                        Code = type.TTE_Codigo,
                        Name = type.TTE_Nombre
                    })
                    .ToListAsync(cancellationToken)
        };
    }

    public async Task<OperationResult<CustomerResponse>> CreateAsync(
        SaveCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        CustomerInput input = CustomerInput.From(request);

        ReferenceData references =
            await LoadReferenceDataAsync(cancellationToken);

        string[] errors = ValidateReferences(input, references);

        if (errors.Length > 0)
        {
            return OperationResult<CustomerResponse>.Failure(errors);
        }

        // El código no se ingresa manualmente: siempre lo asigna el sistema.
        string code = await GenerateCodeAsync(cancellationToken);

        string? conflict = await FindConflictAsync(
            input,
            code,
            references,
            excludingCustomerId: null,
            excludingPersonId: null,
            cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<CustomerResponse>.Conflict(conflict);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    DateTime now = DateTime.UtcNow;

                    PB_PERSONA person = new()
                    {
                        PER_TipoPersonaId =
                            references.PersonTypeIds[input.PersonType],
                        PER_Activo = true,
                        PER_FechaCreacion = now
                    };

                    ApplyPersonData(person, input);

                    PB_CLIENTE customer = new()
                    {
                        CLI_Codigo = code,
                        Persona = person,
                        CLI_FechaCreacion = now
                    };

                    ApplyCustomerData(customer, input);

                    _context.Clientes.Add(customer);

                    await _context.SaveChangesAsync(token);

                    await SyncContactsAsync(
                        person.PER_Persona,
                        input,
                        references,
                        token);

                    return OperationResult<CustomerResponse>.Success(
                        (await GetByIdAsync(customer.CLI_Cliente, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<CustomerResponse>> UpdateAsync(
        int id,
        SaveCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        PB_CLIENTE? customer =
            await _context.Clientes
                .Include(item => item.Persona)
                    .ThenInclude(person => person.TipoPersona)
                .Include(item => item.Persona)
                    .ThenInclude(person => person.PersonaNatural)
                .Include(item => item.Persona)
                    .ThenInclude(person => person.PersonaJuridica)
                .FirstOrDefaultAsync(
                    item => item.CLI_Cliente == id,
                    cancellationToken);

        if (customer is null)
        {
            return OperationResult<CustomerResponse>.Missing(
                "El cliente solicitado no existe.");
        }

        CustomerInput input = CustomerInput.From(request);

        if (!string.Equals(
                customer.Persona.TipoPersona.TPR_Codigo,
                input.PersonType,
                StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<CustomerResponse>.Conflict(
                "No se puede cambiar el tipo de persona de un cliente existente.");
        }

        ReferenceData references =
            await LoadReferenceDataAsync(cancellationToken);

        string[] errors = ValidateReferences(input, references);

        if (errors.Length > 0)
        {
            return OperationResult<CustomerResponse>.Failure(errors);
        }

        string code = customer.CLI_Codigo;

        string? conflict = await FindConflictAsync(
            input,
            code,
            references,
            excludingCustomerId: customer.CLI_Cliente,
            excludingPersonId: customer.CLI_PersonaId,
            cancellationToken);

        if (conflict is not null)
        {
            return OperationResult<CustomerResponse>.Conflict(conflict);
        }

        try
        {
            return await _context.ExecuteInTransactionAsync(
                async token =>
                {
                    DateTime now = DateTime.UtcNow;

                    customer.CLI_Codigo = code;
                    customer.CLI_FechaModificacion = now;
                    customer.Persona.PER_FechaModificacion = now;

                    ApplyPersonData(customer.Persona, input);
                    ApplyCustomerData(customer, input);

                    await _context.SaveChangesAsync(token);

                    await SyncContactsAsync(
                        customer.CLI_PersonaId,
                        input,
                        references,
                        token);

                    return OperationResult<CustomerResponse>.Success(
                        (await GetByIdAsync(customer.CLI_Cliente, token))!);
                },
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.IsUniqueViolation())
        {
            return ConcurrentConflict();
        }
    }

    public async Task<OperationResult<CustomerResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_CLIENTE? customer =
            await _context.Clientes.FirstOrDefaultAsync(
                item => item.CLI_Cliente == id,
                cancellationToken);

        if (customer is null)
        {
            return OperationResult<CustomerResponse>.Missing(
                "El cliente solicitado no existe.");
        }

        // Eliminación lógica: el cliente se conserva para ventas
        // e históricos.
        customer.CLI_Activo = isActive;
        customer.CLI_FechaModificacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<CustomerResponse>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    /// <summary>
    /// Reemplaza los contactos principales. Los reemplazados quedan
    /// inactivos; si un valor anterior vuelve a usarse se reactiva.
    /// Se guarda en dos pasos para respetar los índices únicos de
    /// "un principal activo por persona".
    /// </summary>
    private async Task SyncContactsAsync(
        int personId,
        CustomerInput input,
        ReferenceData references,
        CancellationToken cancellationToken)
    {
        List<PB_PERSONA_IDENTIFICACION> identifications =
            await _context.PersonasIdentificaciones
                .Where(item => item.PID_PersonaId == personId)
                .ToListAsync(cancellationToken);

        List<PB_PERSONA_CORREO> emails =
            await _context.PersonasCorreos
                .Where(item => item.PCO_PersonaId == personId)
                .ToListAsync(cancellationToken);

        List<PB_PERSONA_TELEFONO> phones =
            await _context.PersonasTelefonos
                .Where(item => item.PTE_PersonaId == personId)
                .ToListAsync(cancellationToken);

        foreach (PB_PERSONA_IDENTIFICACION item in identifications.Where(item => item.PID_Activo))
        {
            bool isNit = item.PID_TipoIdentificacionId == references.NitTypeId;

            bool keep = isNit
                ? item.PID_Numero == input.Nit
                : item.PID_TipoIdentificacionId == input.IdentificationTypeId &&
                  item.PID_Numero == input.IdentificationNumber;

            item.PID_Principal = keep && item.PID_Principal && !isNit;
            item.PID_Activo = keep;
        }

        foreach (PB_PERSONA_CORREO item in emails.Where(item => item.PCO_Activo))
        {
            bool keep = string.Equals(
                item.PCO_Correo,
                input.Email,
                StringComparison.OrdinalIgnoreCase);

            item.PCO_Principal = keep && item.PCO_Principal;
            item.PCO_Activo = keep;
        }

        foreach (PB_PERSONA_TELEFONO item in phones.Where(item => item.PTE_Activo))
        {
            bool keep = item.PTE_Numero == input.Phone;

            item.PTE_Principal = keep && item.PTE_Principal;
            item.PTE_Activo = keep;
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (input.IdentificationNumber is not null)
        {
            UpsertIdentification(
                identifications,
                personId,
                input.IdentificationTypeId!.Value,
                input.IdentificationNumber,
                principal: true);
        }

        if (input.Nit is not null)
        {
            UpsertIdentification(
                identifications,
                personId,
                references.NitTypeId,
                input.Nit,
                principal: false);
        }

        if (input.Email is not null)
        {
            PB_PERSONA_CORREO? email = emails.FirstOrDefault(item =>
                string.Equals(
                    item.PCO_Correo,
                    input.Email,
                    StringComparison.OrdinalIgnoreCase));

            if (email is null)
            {
                _context.PersonasCorreos.Add(new PB_PERSONA_CORREO
                {
                    PCO_PersonaId = personId,
                    PCO_Correo = input.Email,
                    PCO_Principal = true,
                    PCO_Activo = true
                });
            }
            else
            {
                email.PCO_Correo = input.Email;
                email.PCO_Principal = true;
                email.PCO_Activo = true;
            }
        }

        if (input.Phone is not null)
        {
            PB_PERSONA_TELEFONO? phone = phones.FirstOrDefault(item =>
                item.PTE_Numero == input.Phone);

            if (phone is null)
            {
                _context.PersonasTelefonos.Add(new PB_PERSONA_TELEFONO
                {
                    PTE_PersonaId = personId,
                    PTE_TipoTelefonoId = input.PhoneTypeId!.Value,
                    PTE_Numero = input.Phone,
                    PTE_Extension = input.PhoneExtension,
                    PTE_Principal = true,
                    PTE_Activo = true
                });
            }
            else
            {
                phone.PTE_TipoTelefonoId = input.PhoneTypeId!.Value;
                phone.PTE_Extension = input.PhoneExtension;
                phone.PTE_Principal = true;
                phone.PTE_Activo = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private void UpsertIdentification(
        List<PB_PERSONA_IDENTIFICACION> identifications,
        int personId,
        int typeId,
        string number,
        bool principal)
    {
        PB_PERSONA_IDENTIFICACION? identification =
            identifications.FirstOrDefault(item =>
                item.PID_TipoIdentificacionId == typeId &&
                item.PID_Numero == number);

        if (identification is null)
        {
            _context.PersonasIdentificaciones.Add(new PB_PERSONA_IDENTIFICACION
            {
                PID_PersonaId = personId,
                PID_TipoIdentificacionId = typeId,
                PID_Numero = number,
                PID_Principal = principal,
                PID_Activo = true
            });

            return;
        }

        identification.PID_Principal = principal;
        identification.PID_Activo = true;
    }

    /// <summary>
    /// Busca duplicados también en registros inactivos.
    /// </summary>
    private async Task<string?> FindConflictAsync(
        CustomerInput input,
        string code,
        ReferenceData references,
        int? excludingCustomerId,
        int? excludingPersonId,
        CancellationToken cancellationToken)
    {
        var codeOwner =
            await _context.Clientes
                .AsNoTracking()
                .Where(customer =>
                    customer.CLI_Codigo == code &&
                    customer.CLI_Cliente != excludingCustomerId)
                .Select(customer => new { customer.CLI_Activo })
                .FirstOrDefaultAsync(cancellationToken);

        if (codeOwner is not null)
        {
            return $"El código {code} ya está registrado" +
                   (codeOwner.CLI_Activo ? "." : " en un cliente inactivo.");
        }

        if (input.IdentificationNumber is not null)
        {
            string? owner = await FindIdentificationOwnerAsync(
                input.IdentificationTypeId!.Value,
                input.IdentificationNumber,
                excludingPersonId,
                cancellationToken);

            if (owner is not null)
            {
                return $"La identificación {input.IdentificationNumber} " +
                       $"ya está registrada{owner}.";
            }
        }

        if (input.Nit is not null)
        {
            string? owner = await FindIdentificationOwnerAsync(
                references.NitTypeId,
                input.Nit,
                excludingPersonId,
                cancellationToken);

            if (owner is not null)
            {
                return $"El NIT {input.Nit} ya está registrado{owner}.";
            }
        }

        if (input.Email is not null)
        {
            var emailOwner =
                await _context.PersonasCorreos
                    .AsNoTracking()
                    .Where(item =>
                        item.PCO_Activo &&
                        item.PCO_Correo == input.Email &&
                        item.PCO_PersonaId != excludingPersonId &&
                        item.Persona.Cliente != null)
                    .Select(item => new
                    {
                        item.Persona.Cliente!.CLI_Codigo,
                        item.Persona.Cliente.CLI_Activo
                    })
                    .FirstOrDefaultAsync(cancellationToken);

            if (emailOwner is not null)
            {
                return $"El correo {input.Email} ya está registrado para el cliente " +
                       $"{emailOwner.CLI_Codigo}" +
                       (emailOwner.CLI_Activo ? "." : " (inactivo).");
            }
        }

        return null;
    }

    /// <summary>
    /// El índice único (tipo, número) abarca a todas las personas,
    /// incluidos registros inactivos y proveedores.
    /// </summary>
    private async Task<string?> FindIdentificationOwnerAsync(
        int typeId,
        string number,
        int? excludingPersonId,
        CancellationToken cancellationToken)
    {
        var owner =
            await _context.PersonasIdentificaciones
                .AsNoTracking()
                .Where(item =>
                    item.PID_TipoIdentificacionId == typeId &&
                    item.PID_Numero == number &&
                    item.PID_PersonaId != excludingPersonId)
                .Select(item => new
                {
                    item.PID_Activo,
                    CustomerCode = item.Persona.Cliente != null
                        ? item.Persona.Cliente.CLI_Codigo
                        : null,
                    CustomerActive = item.Persona.Cliente != null &&
                                     item.Persona.Cliente.CLI_Activo
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (owner is null)
        {
            return null;
        }

        string description = owner.CustomerCode is null
            ? " para otra persona"
            : $" para el cliente {owner.CustomerCode}";

        bool inactive = !owner.PID_Activo ||
                        (owner.CustomerCode is not null && !owner.CustomerActive);

        return description + (inactive ? " (registro inactivo)" : string.Empty);
    }

    private async Task<string> GenerateCodeAsync(
        CancellationToken cancellationToken)
    {
        int next =
            (await _context.Clientes.MaxAsync(
                customer => (int?)customer.CLI_Cliente,
                cancellationToken) ?? 0) + 1;

        while (true)
        {
            string code = $"{CodePrefix}{next:D6}";

            bool exists = await _context.Clientes.AnyAsync(
                customer => customer.CLI_Codigo == code,
                cancellationToken);

            if (!exists)
            {
                return code;
            }

            next++;
        }
    }

    private async Task<ReferenceData> LoadReferenceDataAsync(
        CancellationToken cancellationToken)
    {
        Dictionary<string, int> personTypes =
            await _context.TiposPersona
                .AsNoTracking()
                .Where(type => type.TPR_Activo)
                .ToDictionaryAsync(
                    type => type.TPR_Codigo,
                    type => type.TPR_TipoPersona,
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);

        Dictionary<int, PB_TIPO_IDENTIFICACION> identificationTypes =
            await _context.TiposIdentificacion
                .AsNoTracking()
                .ToDictionaryAsync(
                    type => type.TID_TipoIdentificacion,
                    cancellationToken);

        HashSet<int> phoneTypes =
            (await _context.TiposTelefono
                .AsNoTracking()
                .Where(type => type.TTE_Activo)
                .Select(type => type.TTE_TipoTelefono)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        PB_TIPO_IDENTIFICACION nitType =
            identificationTypes.Values.FirstOrDefault(type =>
                type.TID_Codigo == NitCode)
            ?? throw new InvalidOperationException(
                "No existe el tipo de identificación NIT en los datos iniciales.");

        return new ReferenceData(
            personTypes,
            identificationTypes,
            phoneTypes,
            nitType);
    }

    private static string[] ValidateReferences(
        CustomerInput input,
        ReferenceData references)
    {
        List<string> errors = [];

        if (!references.PersonTypeIds.ContainsKey(input.PersonType))
        {
            errors.Add("El tipo de persona no existe o está inactivo.");
        }

        if (input.IdentificationNumber is not null)
        {
            if (!references.IdentificationTypes.TryGetValue(
                    input.IdentificationTypeId!.Value,
                    out PB_TIPO_IDENTIFICACION? type) ||
                !type.TID_Activo ||
                type.TID_TipoIdentificacion == references.NitTypeId)
            {
                errors.Add("El tipo de identificación no existe o está inactivo.");
            }
            else
            {
                AddLengthError(errors, type, input.IdentificationNumber);

                if (type.TID_Codigo == DpiCode &&
                    !ContactRules.IsDigitsOnly(input.IdentificationNumber))
                {
                    errors.Add("El DPI solo admite dígitos.");
                }
            }
        }

        if (input.Nit is not null)
        {
            AddLengthError(errors, references.NitType, input.Nit);
        }

        if (input.Phone is not null &&
            !references.PhoneTypeIds.Contains(input.PhoneTypeId!.Value))
        {
            errors.Add("El tipo de teléfono no existe o está inactivo.");
        }

        return errors.ToArray();
    }

    private static void AddLengthError(
        List<string> errors,
        PB_TIPO_IDENTIFICACION type,
        string number)
    {
        if ((type.TID_LongitudMinima.HasValue &&
             number.Length < type.TID_LongitudMinima) ||
            (type.TID_LongitudMaxima.HasValue &&
             number.Length > type.TID_LongitudMaxima))
        {
            string range = type.TID_LongitudMinima == type.TID_LongitudMaxima
                ? $"{type.TID_LongitudMinima}"
                : $"entre {type.TID_LongitudMinima} y {type.TID_LongitudMaxima}";

            errors.Add($"El {type.TID_Codigo} debe tener {range} caracteres.");
        }
    }

    private static void ApplyPersonData(
        PB_PERSONA person,
        CustomerInput input)
    {
        if (input.PersonType == SaveCustomerRequest.NaturalPerson)
        {
            person.PersonaNatural ??= new PB_PERSONA_NATURAL();
            person.PersonaNatural.PNA_PrimerNombre = input.FirstName!;
            person.PersonaNatural.PNA_SegundoNombre = input.MiddleName;
            person.PersonaNatural.PNA_PrimerApellido = input.LastName!;
            person.PersonaNatural.PNA_SegundoApellido = input.SecondLastName;
            person.PersonaNatural.PNA_FechaNacimiento = input.BirthDate;
            return;
        }

        person.PersonaJuridica ??= new PB_PERSONA_JURIDICA();
        person.PersonaJuridica.PJU_RazonSocial = input.LegalName!;
        person.PersonaJuridica.PJU_NombreComercial = input.TradeName;
    }

    private static void ApplyCustomerData(
        PB_CLIENTE customer,
        CustomerInput input)
    {
        customer.CLI_LimiteCredito = input.CreditLimit;
        customer.CLI_DiasCredito = input.CreditDays;
        customer.CLI_Observaciones = input.Notes;
    }

    private static string BuildDisplayName(
        string? firstName,
        string? middleName,
        string? lastName,
        string? secondLastName,
        string? legalName)
    {
        if (!string.IsNullOrWhiteSpace(legalName))
        {
            return legalName;
        }

        return string.Join(
            " ",
            new[] { firstName, middleName, lastName, secondLastName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static OperationResult<CustomerResponse> ConcurrentConflict()
    {
        return OperationResult<CustomerResponse>.Conflict(
            "Otro registro con el mismo código o identificación se guardó " +
            "al mismo tiempo. Revise los datos e intente nuevamente.");
    }

    private sealed record ReferenceData(
        Dictionary<string, int> PersonTypeIds,
        Dictionary<int, PB_TIPO_IDENTIFICACION> IdentificationTypes,
        HashSet<int> PhoneTypeIds,
        PB_TIPO_IDENTIFICACION NitType)
    {
        public int NitTypeId => NitType.TID_TipoIdentificacion;
    }

    /// <summary>
    /// Valores del request ya normalizados; los vacíos quedan en null.
    /// </summary>
    private sealed record CustomerInput(
        string PersonType,
        string? FirstName,
        string? MiddleName,
        string? LastName,
        string? SecondLastName,
        DateOnly? BirthDate,
        string? LegalName,
        string? TradeName,
        int? IdentificationTypeId,
        string? IdentificationNumber,
        string? Nit,
        string? Email,
        int? PhoneTypeId,
        string? Phone,
        string? PhoneExtension,
        decimal CreditLimit,
        int CreditDays,
        string? Notes)
    {
        public static CustomerInput From(SaveCustomerRequest request)
        {
            string personType = request.PersonType.Trim().ToUpperInvariant();
            bool natural = personType == SaveCustomerRequest.NaturalPerson;
            string? identification = Clean(request.IdentificationNumber);
            string? phone = Clean(request.Phone);

            return new CustomerInput(
                personType,
                natural ? Clean(request.FirstName) : null,
                natural ? Clean(request.MiddleName) : null,
                natural ? Clean(request.LastName) : null,
                natural ? Clean(request.SecondLastName) : null,
                natural ? request.BirthDate : null,
                natural ? null : Clean(request.LegalName),
                natural ? null : Clean(request.TradeName),
                identification is null ? null : request.IdentificationTypeId,
                identification is null
                    ? null
                    : ContactRules.NormalizeIdentification(identification),
                Clean(request.Nit) is { } nit
                    ? ContactRules.NormalizeIdentification(nit)
                    : null,
                Clean(request.Email) is { } email
                    ? ContactRules.NormalizeEmail(email)
                    : null,
                phone is null ? null : request.PhoneTypeId,
                phone is null ? null : ContactRules.NormalizePhone(phone),
                phone is null ? null : Clean(request.PhoneExtension),
                request.CreditLimit,
                request.CreditDays,
                Clean(request.Notes));
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
