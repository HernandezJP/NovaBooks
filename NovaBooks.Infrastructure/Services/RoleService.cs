using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Data;
using NovaBooks.Infrastructure.Data.Identity;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Infrastructure.Services;

public sealed class RoleService : IRoleService
{
    private const string AdministratorRole = "Administrador";

    private readonly AppDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleService(
        AppDbContext context,
        RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await QueryRolesAsync(
            roleId: null,
            cancellationToken);
    }

    public async Task<RoleResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return (await QueryRolesAsync(id, cancellationToken))
            .SingleOrDefault();
    }

    public async Task<OperationResult<IReadOnlyCollection<RoleUserResponse>>>
        GetUsersAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        bool exists =
            await _context.Roles.AnyAsync(
                role => role.Id == id,
                cancellationToken);

        if (!exists)
        {
            return OperationResult<IReadOnlyCollection<RoleUserResponse>>
                .Missing("El rol solicitado no existe.");
        }

        List<RoleUserResponse> users =
            await (from userRole in _context.UserRoles
                   join user in _context.Users
                       on userRole.UserId equals user.Id
                   where userRole.RoleId == id
                   orderby user.UserName
                   select new RoleUserResponse
                   {
                       Id = user.Id,
                       UserName = user.UserName ?? string.Empty,
                       Email = user.Email ?? string.Empty,
                       IsActive = user.USU_Activo
                   })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<RoleUserResponse>>
            .Success(users);
    }

    public async Task<OperationResult<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string roleName = request.Name.Trim();

        // RoleExistsAsync también considera roles inactivos.
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return OperationResult<RoleResponse>.Conflict(
                "Ya existe un rol con ese nombre.");
        }

        ApplicationRole role = new()
        {
            Name = roleName,
            ROL_Descripcion = NormalizeOptional(request.Description),
            ROL_Activo = true,
            ROL_FechaCreacion = DateTime.UtcNow
        };

        IdentityResult result =
            await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return IdentityFailure<RoleResponse>(result);
        }

        return OperationResult<RoleResponse>.Success(
            (await GetByIdAsync(role.Id, cancellationToken))!);
    }

    public async Task<OperationResult<RoleResponse>> UpdateAsync(
        int id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationRole? role =
            await _roleManager.FindByIdAsync(id.ToString());

        if (role is null)
        {
            return OperationResult<RoleResponse>.Missing(
                "El rol solicitado no existe.");
        }

        string newName = request.Name.Trim();

        bool renames = !string.Equals(
            role.Name,
            newName,
            StringComparison.Ordinal);

        if (renames && IsProtected(role))
        {
            return OperationResult<RoleResponse>.Conflict(
                "El rol Administrador no puede renombrarse.");
        }

        ApplicationRole? existingRole =
            await _roleManager.FindByNameAsync(newName);

        if (existingRole is not null &&
            existingRole.Id != role.Id)
        {
            return OperationResult<RoleResponse>.Conflict(
                "Ya existe un rol con ese nombre.");
        }

        role.Name = newName;
        role.ROL_Descripcion = NormalizeOptional(request.Description);
        role.ROL_FechaModificacion = DateTime.UtcNow;

        IdentityResult result =
            await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return IdentityFailure<RoleResponse>(result);
        }

        return OperationResult<RoleResponse>.Success(
            (await GetByIdAsync(role.Id, cancellationToken))!);
    }

    public async Task<OperationResult<RoleResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationRole? role =
            await _roleManager.FindByIdAsync(id.ToString());

        if (role is null)
        {
            return OperationResult<RoleResponse>.Missing(
                "El rol solicitado no existe.");
        }

        if (!isActive)
        {
            if (IsProtected(role))
            {
                return OperationResult<RoleResponse>.Conflict(
                    "El rol Administrador no puede ser desactivado.");
            }

            int usersCount =
                await _context.UserRoles.CountAsync(
                    userRole => userRole.RoleId == role.Id,
                    cancellationToken);

            if (usersCount > 0)
            {
                return OperationResult<RoleResponse>.Conflict(
                    "No se puede desactivar el rol porque está " +
                    $"asignado a {usersCount} usuario(s).");
            }
        }

        role.ROL_Activo = isActive;
        role.ROL_FechaModificacion = DateTime.UtcNow;

        IdentityResult result =
            await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return IdentityFailure<RoleResponse>(result);
        }

        return OperationResult<RoleResponse>.Success(
            (await GetByIdAsync(role.Id, cancellationToken))!);
    }

    public async Task<OperationResult<RoleResponse>>
        AssignPermissionsAsync(
            int id,
            AssignRolePermissionsRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationRole? role =
            await _roleManager.FindByIdAsync(id.ToString());

        if (role is null)
        {
            return OperationResult<RoleResponse>.Missing(
                "El rol solicitado no existe.");
        }

        if (IsProtected(role))
        {
            return OperationResult<RoleResponse>.Conflict(
                "Los permisos del rol Administrador " +
                "no pueden ser modificados.");
        }

        Dictionary<string, string> knownPermissions =
            SystemPermissions.GetAll().ToDictionary(
                permission => permission,
                permission => permission,
                StringComparer.OrdinalIgnoreCase);

        string[] requested = request.Permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] unknown = requested
            .Where(permission => !knownPermissions.ContainsKey(permission))
            .ToArray();

        if (unknown.Length > 0)
        {
            return OperationResult<RoleResponse>.Failure(
                "Los siguientes permisos no existen: " +
                string.Join(", ", unknown));
        }

        HashSet<string> target = new(
            requested.Select(permission => knownPermissions[permission]),
            StringComparer.OrdinalIgnoreCase);

        List<IdentityRoleClaim<int>> existingClaims =
            await _context.RoleClaims
                .Where(claim =>
                    claim.RoleId == role.Id &&
                    claim.ClaimType == CustomClaimTypes.Permission)
                .ToListAsync(cancellationToken);

        // Se conserva una sola fila por permiso solicitado; se eliminan
        // las demás (incluidos duplicados históricos).
        HashSet<string> kept = new(StringComparer.OrdinalIgnoreCase);

        foreach (IdentityRoleClaim<int> claim in existingClaims)
        {
            if (claim.ClaimValue is not null &&
                target.Contains(claim.ClaimValue) &&
                kept.Add(claim.ClaimValue))
            {
                continue;
            }

            _context.RoleClaims.Remove(claim);
        }

        foreach (string permission in target.Except(kept))
        {
            _context.RoleClaims.Add(new IdentityRoleClaim<int>
            {
                RoleId = role.Id,
                ClaimType = CustomClaimTypes.Permission,
                ClaimValue = permission
            });
        }

        role.ROL_FechaModificacion = DateTime.UtcNow;

        // Un único SaveChanges: el reemplazo es atómico.
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<RoleResponse>.Success(
            (await GetByIdAsync(role.Id, cancellationToken))!);
    }

    private async Task<IReadOnlyCollection<RoleResponse>> QueryRolesAsync(
        int? roleId,
        CancellationToken cancellationToken)
    {
        IQueryable<ApplicationRole> rolesQuery =
            _context.Roles.AsNoTracking();

        IQueryable<IdentityRoleClaim<int>> claimsQuery =
            _context.RoleClaims.AsNoTracking();

        if (roleId.HasValue)
        {
            rolesQuery = rolesQuery.Where(role => role.Id == roleId);
            claimsQuery = claimsQuery.Where(claim => claim.RoleId == roleId);
        }

        var roles =
            await rolesQuery
                .OrderBy(role => role.Name)
                .Select(role => new
                {
                    role.Id,
                    role.Name,
                    role.ROL_Descripcion,
                    role.ROL_Activo,
                    UsersCount = _context.UserRoles.Count(
                        userRole => userRole.RoleId == role.Id)
                })
                .ToListAsync(cancellationToken);

        var claims =
            await claimsQuery
                .Where(claim =>
                    claim.ClaimType == CustomClaimTypes.Permission &&
                    claim.ClaimValue != null)
                .Select(claim => new
                {
                    claim.RoleId,
                    claim.ClaimValue
                })
                .ToListAsync(cancellationToken);

        ILookup<int, string> permissionsByRole =
            claims.ToLookup(
                claim => claim.RoleId,
                claim => claim.ClaimValue!);

        return roles
            .Select(role => new RoleResponse
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.ROL_Descripcion,
                IsActive = role.ROL_Activo,
                IsProtected = IsAdministratorName(role.Name),
                UsersCount = role.UsersCount,
                Permissions = permissionsByRole[role.Id]
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(permission => permission)
                    .ToArray()
            })
            .ToArray();
    }

    private static bool IsProtected(ApplicationRole role)
    {
        return IsAdministratorName(role.Name);
    }

    private static bool IsAdministratorName(string? roleName)
    {
        return string.Equals(
            roleName,
            AdministratorRole,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static OperationResult<T> IdentityFailure<T>(
        IdentityResult identityResult)
    {
        string[] errors = identityResult.Errors
            .Select(error => error.Description)
            .Distinct()
            .ToArray();

        return OperationResult<T>.Failure(errors);
    }
}
