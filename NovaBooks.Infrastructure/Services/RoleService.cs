using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Data.Identity;
using NovaBooks.Infrastructure.Security.Permissions;
using System.Security.Claims;

namespace NovaBooks.Infrastructure.Services;

public sealed class RoleService : IRoleService
{
    private const string AdministratorRole = "Administrador";

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        List<ApplicationRole> roles =
            await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(role => role.Name)
                .ToListAsync(cancellationToken);

        List<RoleResponse> responses = [];

        foreach (ApplicationRole role in roles)
        {
            responses.Add(await MapAsync(role));
        }

        return responses;
    }

    public async Task<RoleResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationRole? role =
            await _roleManager.FindByIdAsync(
                id.ToString());

        if (role is null)
        {
            return null;
        }

        return await MapAsync(role);
    }

    public async Task<RoleOperationResult<RoleResponse>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string roleName = request.Name.Trim();

        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return RoleOperationResult<RoleResponse>.Failure(
                "Ya existe un rol con ese nombre.");
        }

        ApplicationRole role = new()
        {
            Name = roleName
        };

        IdentityResult createResult =
            await _roleManager.CreateAsync(role);

        if (!createResult.Succeeded)
        {
            return IdentityFailure<RoleResponse>(
                createResult);
        }

        return RoleOperationResult<RoleResponse>.Success(
            await MapAsync(role));
    }

    public async Task<RoleOperationResult<RoleResponse>> UpdateAsync(
        int id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationRole? role =
            await _roleManager.FindByIdAsync(
                id.ToString());

        if (role is null)
        {
            return RoleOperationResult<RoleResponse>.Missing(
                "El rol solicitado no existe.");
        }

        if (IsAdministratorRole(role))
        {
            return RoleOperationResult<RoleResponse>.Failure(
                "El rol Administrador no puede ser modificado.");
        }

        string newName = request.Name.Trim();

        ApplicationRole? existingRole =
            await _roleManager.FindByNameAsync(newName);

        if (existingRole is not null &&
            existingRole.Id != role.Id)
        {
            return RoleOperationResult<RoleResponse>.Failure(
                "Ya existe un rol con ese nombre.");
        }

        role.Name = newName;

        IdentityResult updateResult =
            await _roleManager.UpdateAsync(role);

        if (!updateResult.Succeeded)
        {
            return IdentityFailure<RoleResponse>(
                updateResult);
        }

        return RoleOperationResult<RoleResponse>.Success(
            await MapAsync(role));
    }

    public async Task<RoleOperationResult<bool>> DeleteAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationRole? role =
            await _roleManager.FindByIdAsync(
                id.ToString());

        if (role is null)
        {
            return RoleOperationResult<bool>.Missing(
                "El rol solicitado no existe.");
        }

        if (IsAdministratorRole(role))
        {
            return RoleOperationResult<bool>.Failure(
                "El rol Administrador no puede ser desactivado.");
        }

        string roleName =
            role.Name ?? string.Empty;

        IList<ApplicationUser> users =
            await _userManager.GetUsersInRoleAsync(roleName);

        if (users.Count > 0)
        {
            return RoleOperationResult<bool>.Failure(
                "No se puede desactivar el rol porque está " +
                $"asignado a {users.Count} usuario(s).");
        }

        role.ROL_Activo = false;

        IdentityResult result =
            await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return IdentityFailure<bool>(result);
        }

        return RoleOperationResult<bool>.Success(true);
    }

    public async Task<RoleOperationResult<RoleResponse>>
        AssignPermissionsAsync(
            int id,
            AssignRolePermissionsRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationRole? role =
            await _roleManager.FindByIdAsync(
                id.ToString());

        if (role is null)
        {
            return RoleOperationResult<RoleResponse>.Missing(
                "El rol solicitado no existe.");
        }

        if (IsAdministratorRole(role))
        {
            return RoleOperationResult<RoleResponse>.Failure(
                "Los permisos del rol Administrador " +
                "no pueden ser modificados.");
        }

        string[] requestedPermissions =
            request.Permissions
                .Where(permission =>
                    !string.IsNullOrWhiteSpace(permission))
                .Select(permission => permission.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        HashSet<string> validPermissions = new(
            SystemPermissions.GetAll(),
            StringComparer.OrdinalIgnoreCase);

        string[] invalidPermissions =
            requestedPermissions
                .Where(permission =>
                    !validPermissions.Contains(permission))
                .ToArray();

        if (invalidPermissions.Length > 0)
        {
            return RoleOperationResult<RoleResponse>.Failure(
                "Los siguientes permisos no existen: " +
                string.Join(", ", invalidPermissions));
        }

        IList<Claim> existingClaims =
            await _roleManager.GetClaimsAsync(role);

        Claim[] existingPermissionClaims =
            existingClaims
                .Where(claim =>
                    claim.Type.Equals(
                        CustomClaimTypes.Permission,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        string[] existingPermissionValues =
            existingPermissionClaims
                .Select(claim => claim.Value)
                .ToArray();

        foreach (Claim claim in existingPermissionClaims)
        {
            IdentityResult removeResult =
                await _roleManager.RemoveClaimAsync(
                    role,
                    claim);

            if (!removeResult.Succeeded)
            {
                await RestorePermissionsAsync(
                    role,
                    existingPermissionValues);

                return IdentityFailure<RoleResponse>(
                    removeResult);
            }
        }

        foreach (string permission in requestedPermissions)
        {
            IdentityResult addResult =
                await _roleManager.AddClaimAsync(
                    role,
                    new Claim(
                        CustomClaimTypes.Permission,
                        permission));

            if (!addResult.Succeeded)
            {
                await RemoveAllPermissionClaimsAsync(role);

                await RestorePermissionsAsync(
                    role,
                    existingPermissionValues);

                return IdentityFailure<RoleResponse>(
                    addResult);
            }
        }

        return RoleOperationResult<RoleResponse>.Success(
            await MapAsync(role));
    }

    private async Task<RoleResponse> MapAsync(
        ApplicationRole role)
    {
        IList<Claim> claims =
            await _roleManager.GetClaimsAsync(role);

        string[] permissions =
            claims
                .Where(claim =>
                    claim.Type.Equals(
                        CustomClaimTypes.Permission,
                        StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(permission => permission)
                .ToArray();

        string roleName =
            role.Name ?? string.Empty;

        IList<ApplicationUser> users =
            await _userManager.GetUsersInRoleAsync(roleName);

        return new RoleResponse
        {
            Id = role.Id,
            Name = roleName,
            IsActive = role.ROL_Activo,
            UsersCount = users.Count,
            Permissions = permissions
        };
    }

    private async Task RemoveAllPermissionClaimsAsync(
        ApplicationRole role)
    {
        IList<Claim> claims =
            await _roleManager.GetClaimsAsync(role);

        foreach (Claim claim in claims.Where(claim =>
                     claim.Type.Equals(
                         CustomClaimTypes.Permission,
                         StringComparison.OrdinalIgnoreCase)))
        {
            await _roleManager.RemoveClaimAsync(
                role,
                claim);
        }
    }

    private async Task RestorePermissionsAsync(
        ApplicationRole role,
        IEnumerable<string> permissions)
    {
        foreach (string permission in permissions
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _roleManager.AddClaimAsync(
                role,
                new Claim(
                    CustomClaimTypes.Permission,
                    permission));
        }
    }

    private static bool IsAdministratorRole(
        ApplicationRole role)
    {
        return string.Equals(
            role.Name,
            AdministratorRole,
            StringComparison.OrdinalIgnoreCase);
    }

    private static RoleOperationResult<T>
        IdentityFailure<T>(
            IdentityResult identityResult)
    {
        string[] errors =
            identityResult.Errors
                .Select(error => error.Description)
                .Distinct()
                .ToArray();

        return RoleOperationResult<T>.Failure(errors);
    }
}