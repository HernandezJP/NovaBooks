using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TikalLibros.Domain.Entities.Administration;
using TikalLibros.Domain.Entities.Configuration;
using TikalLibros.Domain.Entities.Inventory;
using TikalLibros.Domain.Entities.People;
using TikalLibros.Infrastructure.Data.Identity;

namespace TikalLibros.Infrastructure.Data.Seed;

/// <summary>
/// Estructura mínima de la organización: puestos, empresa, sucursal y
/// almacén principales (las compras y el inventario los requieren), y el
/// empleado del administrador. Es idempotente.
/// </summary>
public static class OrganizationSeeder
{
    public const string CompanyCode = "TIKAL";
    public const string MainBranchCode = "SUC-01";
    public const string MainWarehouseCode = "ALM-01";
    public const string AdministratorPosition = "Administrador del sistema";

    private static readonly string[] Positions =
    [
        "Gerente general",
        AdministratorPosition,
        "Encargado de compras",
        "Encargado de bodega",
        "Cajero",
        "Vendedor"
    ];

    public static async Task SeedAsync(
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        HashSet<string> existingPositions =
            (await context.Puestos
                .Select(position => position.PUE_Nombre)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string name in Positions.Where(name => !existingPositions.Contains(name)))
        {
            context.Puestos.Add(new PB_PUESTO { PUE_Nombre = name, PUE_Activo = true });
        }

        await context.SaveChangesAsync(cancellationToken);

        PB_EMPRESA? company = await context.Empresas
            .FirstOrDefaultAsync(item => item.EMP_Codigo == CompanyCode, cancellationToken);

        if (company is null)
        {
            int legalPersonType = await context.TiposPersona
                .Where(type => type.TPR_Codigo == "JURIDICA")
                .Select(type => type.TPR_TipoPersona)
                .SingleAsync(cancellationToken);

            int currency = await context.Monedas
                .Where(item => item.MON_Codigo == "GTQ")
                .Select(item => item.MON_Moneda)
                .SingleAsync(cancellationToken);

            company = new PB_EMPRESA
            {
                EMP_Codigo = CompanyCode,
                EMP_NombreComercial = "Tikal Libros",
                EMP_MonedaPredeterminadaId = currency,
                EMP_Activo = true,
                Persona = new PB_PERSONA
                {
                    PER_TipoPersonaId = legalPersonType,
                    PER_Activo = true,
                    PersonaJuridica = new PB_PERSONA_JURIDICA
                    {
                        PJU_RazonSocial = "Tikal Libros, S.A.",
                        PJU_NombreComercial = "Tikal Libros"
                    }
                }
            };

            context.Empresas.Add(company);
            await context.SaveChangesAsync(cancellationToken);
        }

        PB_SUCURSAL? branch = await context.Sucursales
            .FirstOrDefaultAsync(
                item => item.SUC_EmpresaId == company.EMP_Empresa && item.SUC_Codigo == MainBranchCode,
                cancellationToken);

        if (branch is null)
        {
            int municipality = await context.Municipios
                .Where(item => item.MUN_Codigo == "GUA")
                .Select(item => item.MUN_Municipio)
                .FirstAsync(cancellationToken);

            branch = new PB_SUCURSAL
            {
                SUC_EmpresaId = company.EMP_Empresa,
                SUC_Codigo = MainBranchCode,
                SUC_Nombre = "Sucursal central",
                SUC_EsPrincipal = true,
                SUC_Activo = true,
                Direccion = new PB_DIRECCION
                {
                    DIR_MunicipioId = municipality,
                    DIR_Zona = "1",
                    DIR_Linea1 = "Ciudad de Guatemala",
                    DIR_Activo = true
                }
            };

            context.Sucursales.Add(branch);
            await context.SaveChangesAsync(cancellationToken);
        }

        bool hasWarehouse = await context.Almacenes
            .AnyAsync(item => item.ALM_Codigo == MainWarehouseCode, cancellationToken);

        if (!hasWarehouse)
        {
            context.Almacenes.Add(new PB_ALMACEN
            {
                ALM_SucursalId = branch.SUC_Sucursal,
                ALM_Codigo = MainWarehouseCode,
                ALM_Nombre = "Bodega principal",
                ALM_Descripcion = "Almacén principal de la sucursal central.",
                ALM_EsPrincipal = true,
                ALM_Activo = true
            });

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Vincula el usuario administrador con un empleado para que pueda
    /// registrar compras, recepciones y movimientos de inventario.
    /// </summary>
    public static async Task SeedAdministratorEmployeeAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        string? userName = configuration["IdentitySeed:Administrator:UserName"];

        ApplicationUser? administrator = string.IsNullOrWhiteSpace(userName)
            ? null
            : await userManager.FindByNameAsync(userName);

        if (administrator is null || administrator.USU_EmpleadoId is not null)
        {
            return;
        }

        int naturalPersonType = await context.TiposPersona
            .Where(type => type.TPR_Codigo == "NATURAL")
            .Select(type => type.TPR_TipoPersona)
            .SingleAsync(cancellationToken);

        int position = await context.Puestos
            .Where(item => item.PUE_Nombre == AdministratorPosition)
            .Select(item => item.PUE_Puesto)
            .SingleAsync(cancellationToken);

        int branch = await context.Sucursales
            .Where(item => item.SUC_Codigo == MainBranchCode)
            .Select(item => item.SUC_Sucursal)
            .FirstAsync(cancellationToken);

        int next = await context.Empleados.CountAsync(cancellationToken) + 1;
        string code;

        do
        {
            code = $"EMP-{next++:D4}";
        }
        while (await context.Empleados.AnyAsync(item => item.EMP_Codigo == code, cancellationToken));

        PB_EMPLEADO employee = new()
        {
            EMP_Codigo = code,
            EMP_PuestoId = position,
            EMP_SucursalId = branch,
            EMP_FechaContratacion = DateOnly.FromDateTime(DateTime.UtcNow),
            EMP_Activo = true,
            Persona = new PB_PERSONA
            {
                PER_TipoPersonaId = naturalPersonType,
                PER_Activo = true,
                PersonaNatural = new PB_PERSONA_NATURAL
                {
                    PNA_PrimerNombre = "Administrador",
                    PNA_PrimerApellido = "Sistema"
                }
            }
        };

        context.Empleados.Add(employee);
        await context.SaveChangesAsync(cancellationToken);

        administrator.USU_EmpleadoId = employee.EMP_Empleado;
        await userManager.UpdateAsync(administrator);
    }
}
