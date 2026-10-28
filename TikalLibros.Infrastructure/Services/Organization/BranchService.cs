using Microsoft.EntityFrameworkCore;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.Interfaces;
using TikalLibros.Application.Validation;
using TikalLibros.Domain.Entities.Administration;
using TikalLibros.Domain.Entities.People;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Seed;

namespace TikalLibros.Infrastructure.Services.Organization;

/// <summary>
/// Sucursales de la empresa. Siempre hay una principal y activa (compras e
/// informes la proponen primero); una sucursal solo se desactiva cuando ya no
/// tiene almacenes, cajas ni empleados activos.
/// </summary>
public sealed class BranchService : IBranchService
{
    private const string CodePrefix = "SUC-";

    private readonly AppDbContext _context;

    public BranchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrganizationCatalogsResponse> GetCatalogsAsync(CancellationToken cancellationToken = default)
    {
        return new OrganizationCatalogsResponse
        {
            Departments = await _context.Departamentos
                .AsNoTracking()
                .Where(department => department.DEP_Activo)
                .OrderBy(department => department.DEP_Nombre)
                .Select(department => new DepartmentOptionResponse
                {
                    Id = department.DEP_Departamento,
                    Name = department.DEP_Nombre,
                    Municipalities = department.Municipios
                        .Where(municipality => municipality.MUN_Activo)
                        .OrderBy(municipality => municipality.MUN_Nombre)
                        .Select(municipality => new CatalogOptionResponse
                        {
                            Id = municipality.MUN_Municipio,
                            Code = municipality.MUN_Codigo,
                            Name = municipality.MUN_Nombre
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken),
            Branches = await _context.Sucursales
                .AsNoTracking()
                .Where(branch => branch.SUC_Activo)
                .OrderByDescending(branch => branch.SUC_EsPrincipal)
                .ThenBy(branch => branch.SUC_Nombre)
                .Select(branch => new CatalogOptionResponse { Id = branch.SUC_Sucursal, Code = branch.SUC_Codigo, Name = branch.SUC_Nombre })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<IReadOnlyList<BranchResponse>> GetAllAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PB_SUCURSAL> branches = _context.Sucursales.AsNoTracking();

        if (isActive.HasValue)
        {
            branches = branches.Where(item => item.SUC_Activo == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            branches = branches.Where(item =>
                item.SUC_Nombre.Contains(value) ||
                item.SUC_Codigo.Contains(value) ||
                item.Direccion.Municipio.MUN_Nombre.Contains(value) ||
                item.Direccion.Municipio.Departamento.DEP_Nombre.Contains(value));
        }

        return await Project(branches.OrderByDescending(item => item.SUC_EsPrincipal).ThenBy(item => item.SUC_Nombre))
            .ToListAsync(cancellationToken);
    }

    public Task<BranchResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Project(_context.Sucursales.AsNoTracking().Where(item => item.SUC_Sucursal == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OperationResult<BranchResponse>> CreateAsync(
        SaveBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        return _context.ExecuteInTransactionAsync(async token =>
        {
            BranchInput input = BranchInput.From(request);
            int? companyId = await _context.Empresas
                .Where(company => company.EMP_Codigo == OrganizationSeeder.CompanyCode)
                .Select(company => (int?)company.EMP_Empresa)
                .FirstOrDefaultAsync(token);

            if (companyId is null)
            {
                return OperationResult<BranchResponse>.Failure("No se encontró la empresa a la que pertenece la sucursal.");
            }

            if (await ValidateAsync(input, null, token) is { } error)
            {
                return error;
            }

            bool isMain = input.IsMain || !await _context.Sucursales.AnyAsync(item => item.SUC_EsPrincipal, token);

            if (isMain)
            {
                await ClearMainAsync(token);
            }

            List<string> codes = await _context.Sucursales.Select(item => item.SUC_Codigo).ToListAsync(token);

            PB_SUCURSAL branch = new()
            {
                SUC_EmpresaId = companyId.Value,
                SUC_Codigo = CatalogCodes.NextSequential(CodePrefix, codes),
                SUC_EsPrincipal = isMain,
                SUC_Activo = true,
                SUC_FechaCreacion = DateTime.UtcNow,
                Direccion = new PB_DIRECCION { DIR_Activo = true, DIR_FechaCreacion = DateTime.UtcNow }
            };

            Apply(branch, input);
            _context.Sucursales.Add(branch);

            if (await SaveAsync(token) is { } concurrent)
            {
                return concurrent;
            }

            return OperationResult<BranchResponse>.Success((await GetByIdAsync(branch.SUC_Sucursal, token))!);
        }, cancellationToken);
    }

    public Task<OperationResult<BranchResponse>> UpdateAsync(
        int id,
        SaveBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        return _context.ExecuteInTransactionAsync(async token =>
        {
            PB_SUCURSAL? branch = await _context.Sucursales
                .Include(item => item.Direccion)
                .FirstOrDefaultAsync(item => item.SUC_Sucursal == id, token);

            if (branch is null)
            {
                return OperationResult<BranchResponse>.Missing("La sucursal solicitada no existe.");
            }

            BranchInput input = BranchInput.From(request);

            if (branch.SUC_EsPrincipal && !input.IsMain)
            {
                return OperationResult<BranchResponse>.Failure(
                    "Esta es la sucursal principal. Para cambiarla, marque otra sucursal como principal.");
            }

            if (!branch.SUC_EsPrincipal && input.IsMain && !branch.SUC_Activo)
            {
                return OperationResult<BranchResponse>.Failure("Una sucursal inactiva no puede ser la principal. Reactívela primero.");
            }

            if (await ValidateAsync(input, id, token) is { } error)
            {
                return error;
            }

            if (input.IsMain && !branch.SUC_EsPrincipal)
            {
                await ClearMainAsync(token);
                branch.SUC_EsPrincipal = true;
            }

            Apply(branch, input);
            branch.SUC_FechaModificacion = DateTime.UtcNow;
            branch.Direccion.DIR_FechaModificacion = DateTime.UtcNow;

            if (await SaveAsync(token) is { } concurrent)
            {
                return concurrent;
            }

            return OperationResult<BranchResponse>.Success((await GetByIdAsync(id, token))!);
        }, cancellationToken);
    }

    public async Task<OperationResult<BranchResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        PB_SUCURSAL? branch = await _context.Sucursales.FirstOrDefaultAsync(item => item.SUC_Sucursal == id, cancellationToken);

        if (branch is null)
        {
            return OperationResult<BranchResponse>.Missing("La sucursal solicitada no existe.");
        }

        if (!isActive)
        {
            if (branch.SUC_EsPrincipal)
            {
                return OperationResult<BranchResponse>.Conflict(
                    $"La sucursal {branch.SUC_Nombre} es la principal y no puede desactivarse. Marque otra como principal primero.");
            }

            List<string> inUse = [];

            if (await _context.Almacenes.AnyAsync(item => item.ALM_SucursalId == id && item.ALM_Activo, cancellationToken))
            {
                inUse.Add("almacenes");
            }

            if (await _context.Cajas.AnyAsync(item => item.CAJ_SucursalId == id && item.CAJ_Activo, cancellationToken))
            {
                inUse.Add("cajas");
            }

            if (await _context.Empleados.AnyAsync(item => item.EMP_SucursalId == id && item.EMP_Activo, cancellationToken))
            {
                inUse.Add("empleados");
            }

            if (inUse.Count > 0)
            {
                return OperationResult<BranchResponse>.Conflict(
                    $"La sucursal {branch.SUC_Nombre} todavía tiene {string.Join(", ", inUse)} activos. " +
                    "Desactívelos o trasládelos a otra sucursal primero.");
            }
        }

        branch.SUC_Activo = isActive;
        branch.SUC_FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<BranchResponse>.Success((await GetByIdAsync(id, cancellationToken))!);
    }

    private async Task<OperationResult<BranchResponse>?> ValidateAsync(BranchInput input, int? excludingId, CancellationToken cancellationToken)
    {
        if (!await _context.Municipios.AnyAsync(item => item.MUN_Municipio == input.MunicipalityId && item.MUN_Activo, cancellationToken))
        {
            return OperationResult<BranchResponse>.Failure("El municipio seleccionado no existe o está inactivo.");
        }

        var duplicate = await _context.Sucursales
            .AsNoTracking()
            .Where(item => item.SUC_Sucursal != excludingId && item.SUC_Nombre == input.Name)
            .Select(item => new { item.SUC_Nombre, item.SUC_Activo })
            .FirstOrDefaultAsync(cancellationToken);

        return duplicate is null
            ? null
            : OperationResult<BranchResponse>.Conflict(
                $"Ya existe una sucursal llamada {duplicate.SUC_Nombre}" + (duplicate.SUC_Activo ? "." : " (inactiva; puede reactivarla)."));
    }

    /// <summary>El índice filtrado admite una sola principal: se quita antes de marcar otra.</summary>
    private async Task ClearMainAsync(CancellationToken cancellationToken)
    {
        foreach (PB_SUCURSAL current in await _context.Sucursales.Where(item => item.SUC_EsPrincipal).ToListAsync(cancellationToken))
        {
            current.SUC_EsPrincipal = false;
            current.SUC_FechaModificacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<OperationResult<BranchResponse>?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return OperationResult<BranchResponse>.Conflict(
                "Otra sucursal con el mismo nombre se guardó al mismo tiempo. Revise los datos e intente nuevamente.");
        }
    }

    private static void Apply(PB_SUCURSAL branch, BranchInput input)
    {
        branch.SUC_Nombre = input.Name;
        branch.SUC_Telefono = input.Phone;
        branch.SUC_Correo = input.Email;
        branch.Direccion.DIR_MunicipioId = input.MunicipalityId;
        branch.Direccion.DIR_Zona = input.Zone;
        branch.Direccion.DIR_Linea1 = input.AddressLine;
        branch.Direccion.DIR_Referencia = input.Reference;
    }

    private IQueryable<BranchResponse> Project(IQueryable<PB_SUCURSAL> branches)
    {
        return branches.Select(item => new BranchResponse
        {
            Id = item.SUC_Sucursal,
            Code = item.SUC_Codigo,
            Name = item.SUC_Nombre,
            Phone = item.SUC_Telefono,
            Email = item.SUC_Correo,
            DepartmentId = item.Direccion.Municipio.MUN_DepartamentoId,
            Department = item.Direccion.Municipio.Departamento.DEP_Nombre,
            MunicipalityId = item.Direccion.DIR_MunicipioId,
            Municipality = item.Direccion.Municipio.MUN_Nombre,
            Zone = item.Direccion.DIR_Zona,
            AddressLine = item.Direccion.DIR_Linea1,
            AddressReference = item.Direccion.DIR_Referencia,
            IsMain = item.SUC_EsPrincipal,
            IsActive = item.SUC_Activo,
            WarehousesCount = _context.Almacenes.Count(warehouse => warehouse.ALM_SucursalId == item.SUC_Sucursal && warehouse.ALM_Activo),
            CashRegistersCount = _context.Cajas.Count(register => register.CAJ_SucursalId == item.SUC_Sucursal && register.CAJ_Activo),
            EmployeesCount = item.Empleados.Count(employee => employee.EMP_Activo)
        });
    }

    private sealed record BranchInput(
        string Name,
        string? Phone,
        string? Email,
        int MunicipalityId,
        string? Zone,
        string AddressLine,
        string? Reference,
        bool IsMain)
    {
        public static BranchInput From(SaveBranchRequest request) => new(
            request.Name.Trim(),
            CatalogCodes.Clean(request.Phone) is { } phone ? ContactRules.NormalizePhone(phone) : null,
            CatalogCodes.Clean(request.Email) is { } email ? ContactRules.NormalizeEmail(email) : null,
            request.MunicipalityId ?? 0,
            CatalogCodes.Clean(request.Zone)?.TrimStart('0') is { Length: > 0 } zone ? zone : null,
            request.AddressLine.Trim(),
            CatalogCodes.Clean(request.AddressReference),
            request.IsMain);
    }
}
