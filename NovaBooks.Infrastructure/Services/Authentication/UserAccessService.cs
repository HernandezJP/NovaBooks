using Microsoft.EntityFrameworkCore;
using NovaBooks.Infrastructure.Data;
using NovaBooks.Infrastructure.Data.Identity;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Infrastructure.Services.Authentication;

public sealed record UserAccessSnapshot(
    ApplicationUser User,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions)
{
    public bool IsLockedOut =>
        User.LockoutEnabled &&
        User.LockoutEnd.HasValue &&
        User.LockoutEnd.Value > DateTimeOffset.UtcNow;
}

public interface IUserAccessService
{
    /// <summary>
    /// Obtiene el usuario con sus roles activos y los permisos
    /// vigentes de esos roles y del propio usuario.
    /// </summary>
    Task<UserAccessSnapshot?> GetAccessAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

public sealed class UserAccessService : IUserAccessService
{
    private readonly AppDbContext _context;

    public UserAccessService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserAccessSnapshot?> GetAccessAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user =
            await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user => user.Id == userId,
                    cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles =
            await (from userRole in _context.UserRoles
                   join role in _context.Roles
                       on userRole.RoleId equals role.Id
                   where userRole.UserId == userId &&
                         role.ROL_Activo
                   select new { role.Id, role.Name })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        int[] roleIds = roles
            .Select(role => role.Id)
            .ToArray();

        List<string> rolePermissions =
            await _context.RoleClaims
                .AsNoTracking()
                .Where(claim =>
                    roleIds.Contains(claim.RoleId) &&
                    claim.ClaimType == CustomClaimTypes.Permission &&
                    claim.ClaimValue != null)
                .Select(claim => claim.ClaimValue!)
                .ToListAsync(cancellationToken);

        List<string> userPermissions =
            await _context.UserClaims
                .AsNoTracking()
                .Where(claim =>
                    claim.UserId == userId &&
                    claim.ClaimType == CustomClaimTypes.Permission &&
                    claim.ClaimValue != null)
                .Select(claim => claim.ClaimValue!)
                .ToListAsync(cancellationToken);

        string[] roleNames = roles
            .Where(role => !string.IsNullOrWhiteSpace(role.Name))
            .Select(role => role.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role)
            .ToArray();

        string[] permissions = rolePermissions
            .Concat(userPermissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray();

        return new UserAccessSnapshot(
            user,
            roleNames,
            permissions);
    }
}
