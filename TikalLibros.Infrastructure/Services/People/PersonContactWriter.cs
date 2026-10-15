using Microsoft.EntityFrameworkCore;
using TikalLibros.Domain.Entities.People;
using TikalLibros.Infrastructure.Data;

namespace TikalLibros.Infrastructure.Services.People;

/// <summary>Identificación que debe quedar activa para una persona.</summary>
public sealed record PersonIdentification(int TypeId, string Number, bool Principal);

/// <summary>Correo y teléfono principales que deben quedar activos.</summary>
public sealed record PersonContacts(
    IReadOnlyCollection<PersonIdentification> Identifications,
    string? Email,
    int? PhoneTypeId,
    string? Phone,
    string? PhoneExtension);

/// <summary>
/// Identificaciones y contactos de personas compartidos por clientes,
/// proveedores y empleados.
/// </summary>
public sealed class PersonContactWriter
{
    private readonly AppDbContext _context;

    public PersonContactWriter(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Reemplaza identificaciones y contactos principales. Los reemplazados
    /// quedan inactivos; si un valor anterior vuelve a usarse se reactiva.
    /// Se guarda en dos pasos para respetar los índices únicos de
    /// "un principal activo por persona".
    /// </summary>
    public async Task SyncAsync(
        int personId,
        PersonContacts contacts,
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
            PersonIdentification? desired = contacts.Identifications.FirstOrDefault(wanted =>
                wanted.TypeId == item.PID_TipoIdentificacionId &&
                wanted.Number == item.PID_Numero);

            item.PID_Principal = desired is not null && desired.Principal && item.PID_Principal;
            item.PID_Activo = desired is not null;
        }

        foreach (PB_PERSONA_CORREO item in emails.Where(item => item.PCO_Activo))
        {
            bool keep = string.Equals(item.PCO_Correo, contacts.Email, StringComparison.OrdinalIgnoreCase);

            item.PCO_Principal = keep && item.PCO_Principal;
            item.PCO_Activo = keep;
        }

        foreach (PB_PERSONA_TELEFONO item in phones.Where(item => item.PTE_Activo))
        {
            bool keep = item.PTE_Numero == contacts.Phone;

            item.PTE_Principal = keep && item.PTE_Principal;
            item.PTE_Activo = keep;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (PersonIdentification desired in contacts.Identifications)
        {
            PB_PERSONA_IDENTIFICACION? identification = identifications.FirstOrDefault(item =>
                item.PID_TipoIdentificacionId == desired.TypeId &&
                item.PID_Numero == desired.Number);

            if (identification is null)
            {
                _context.PersonasIdentificaciones.Add(new PB_PERSONA_IDENTIFICACION
                {
                    PID_PersonaId = personId,
                    PID_TipoIdentificacionId = desired.TypeId,
                    PID_Numero = desired.Number,
                    PID_Principal = desired.Principal,
                    PID_Activo = true
                });
            }
            else
            {
                identification.PID_Principal = desired.Principal;
                identification.PID_Activo = true;
            }
        }

        if (contacts.Email is not null)
        {
            PB_PERSONA_CORREO? email = emails.FirstOrDefault(item =>
                string.Equals(item.PCO_Correo, contacts.Email, StringComparison.OrdinalIgnoreCase));

            if (email is null)
            {
                _context.PersonasCorreos.Add(new PB_PERSONA_CORREO
                {
                    PCO_PersonaId = personId,
                    PCO_Correo = contacts.Email,
                    PCO_Principal = true,
                    PCO_Activo = true
                });
            }
            else
            {
                email.PCO_Correo = contacts.Email;
                email.PCO_Principal = true;
                email.PCO_Activo = true;
            }
        }

        if (contacts.Phone is not null)
        {
            PB_PERSONA_TELEFONO? phone = phones.FirstOrDefault(item => item.PTE_Numero == contacts.Phone);

            if (phone is null)
            {
                _context.PersonasTelefonos.Add(new PB_PERSONA_TELEFONO
                {
                    PTE_PersonaId = personId,
                    PTE_TipoTelefonoId = contacts.PhoneTypeId!.Value,
                    PTE_Numero = contacts.Phone,
                    PTE_Extension = contacts.PhoneExtension,
                    PTE_Principal = true,
                    PTE_Activo = true
                });
            }
            else
            {
                phone.PTE_TipoTelefonoId = contacts.PhoneTypeId!.Value;
                phone.PTE_Extension = contacts.PhoneExtension;
                phone.PTE_Principal = true;
                phone.PTE_Activo = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Describe a quién pertenece ya la identificación (" para el cliente
    /// CLI-000001", " para el empleado EMP-0001"…) o null si está libre.
    /// El índice único (tipo, número) abarca a todas las personas, incluidos
    /// registros inactivos.
    /// </summary>
    public async Task<string?> FindIdentificationOwnerAsync(
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
                    CustomerCode = item.Persona.Cliente != null ? item.Persona.Cliente.CLI_Codigo : null,
                    CustomerActive = item.Persona.Cliente != null && item.Persona.Cliente.CLI_Activo,
                    SupplierCode = item.Persona.Proveedor != null ? item.Persona.Proveedor.PRO_Codigo : null,
                    SupplierActive = item.Persona.Proveedor != null && item.Persona.Proveedor.PRO_Activo,
                    EmployeeCode = item.Persona.Empleado != null ? item.Persona.Empleado.EMP_Codigo : null,
                    EmployeeActive = item.Persona.Empleado != null && item.Persona.Empleado.EMP_Activo
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (owner is null)
        {
            return null;
        }

        (string description, bool active) = owner switch
        {
            { CustomerCode: not null } => ($" para el cliente {owner.CustomerCode}", owner.CustomerActive),
            { SupplierCode: not null } => ($" para el proveedor {owner.SupplierCode}", owner.SupplierActive),
            { EmployeeCode: not null } => ($" para el empleado {owner.EmployeeCode}", owner.EmployeeActive),
            _ => (" para otra persona", true)
        };

        return description + (!owner.PID_Activo || !active ? " (registro inactivo)" : string.Empty);
    }
}
