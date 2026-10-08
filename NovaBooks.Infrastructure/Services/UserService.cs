using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Roles;
using NovaBooks.Application.DTOs.Users;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Data;
using NovaBooks.Infrastructure.Data.Identity;

namespace NovaBooks.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private const string AdministratorRole = "Administrador";

    private const string PrincipalAdministratorUserName =
        "admin";

    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PagedResponse<UserResponse>> GetPagedAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;

        pageSize = pageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => pageSize
        };

        IQueryable<ApplicationUser> query =
            _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string value = search.Trim();

            query = query.Where(user =>
                (user.UserName != null &&
                 user.UserName.Contains(value))
                ||
                (user.Email != null &&
                 user.Email.Contains(value))
                ||
                (user.PhoneNumber != null &&
                 user.PhoneNumber.Contains(value)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(user =>
                user.USU_Activo == isActive.Value);
        }

        int totalItems =
            await query.CountAsync(cancellationToken);

        List<ApplicationUser> users =
            await query
                .OrderBy(user => user.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return new PagedResponse<UserResponse>
        {
            Items = await MapManyAsync(users, cancellationToken),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return FindResponseAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleOptionResponse>>
        GetAssignableRolesAsync(
            CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .Where(role => role.ROL_Activo)
            .OrderBy(role => role.Name)
            .Select(role => new RoleOptionResponse
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.ROL_Descripcion
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string userName = request.UserName.Trim();
        string email = request.Email.Trim();

        OperationResult<UserResponse>? duplicate =
            await CheckDuplicatesAsync(
                userName,
                email,
                excludingUserId: null);

        if (duplicate is not null)
        {
            return duplicate;
        }

        string[] requestedRoles = NormalizeRoles(request.Roles);

        string[] roleErrors =
            await ValidateRolesAsync(
                requestedRoles,
                cancellationToken);

        if (roleErrors.Length > 0)
        {
            return OperationResult<UserResponse>.Failure(roleErrors);
        }

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                ApplicationUser user = new()
                {
                    UserName = userName,
                    Email = email,
                    PhoneNumber =
                        NormalizeOptional(request.PhoneNumber),
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    USU_Activo = true,
                    USU_FechaCreacion = DateTime.UtcNow,
                    USU_CreadoPorUsuarioId = authenticatedUserId
                };

                IdentityResult result =
                    await _userManager.CreateAsync(
                        user,
                        request.Password);

                if (result.Succeeded && requestedRoles.Length > 0)
                {
                    result = await _userManager.AddToRolesAsync(
                        user,
                        requestedRoles);
                }

                if (!result.Succeeded)
                {
                    return IdentityFailure<UserResponse>(result);
                }

                return OperationResult<UserResponse>.Success(
                    (await FindResponseAsync(user.Id, token))!);
            },
            cancellationToken);
    }

    public async Task<OperationResult<UserResponse>> UpdateAsync(
        int id,
        UpdateUserRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return OperationResult<UserResponse>.Missing(
                "El usuario solicitado no existe.");
        }

        string newUserName = request.UserName.Trim();
        string newEmail = request.Email.Trim();

        if (IsPrincipalAdministrator(user) &&
            !string.Equals(
                user.UserName,
                newUserName,
                StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<UserResponse>.Conflict(
                "El nombre de usuario del administrador principal " +
                "no puede modificarse.");
        }

        OperationResult<UserResponse>? duplicate =
            await CheckDuplicatesAsync(
                newUserName,
                newEmail,
                excludingUserId: user.Id);

        if (duplicate is not null)
        {
            return duplicate;
        }

        user.UserName = newUserName;
        user.Email = newEmail;
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.USU_FechaModificacion = DateTime.UtcNow;
        user.USU_ModificadoPorUsuarioId = authenticatedUserId;

        IdentityResult result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return IdentityFailure<UserResponse>(result);
        }

        return OperationResult<UserResponse>.Success(
            (await FindResponseAsync(user.Id, cancellationToken))!);
    }

    public async Task<OperationResult<UserResponse>> ChangeStatusAsync(
        int id,
        bool isActive,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return OperationResult<UserResponse>.Missing(
                "El usuario solicitado no existe.");
        }

        if (!isActive)
        {
            if (id == authenticatedUserId)
            {
                return OperationResult<UserResponse>.Conflict(
                    "No puede desactivar su propio usuario.");
            }

            if (IsPrincipalAdministrator(user))
            {
                return OperationResult<UserResponse>.Conflict(
                    "El administrador principal no puede " +
                    "ser desactivado.");
            }

            if (await _userManager.IsInRoleAsync(
                    user,
                    AdministratorRole) &&
                await CountOtherActiveAdministratorsAsync(
                    user.Id,
                    cancellationToken) == 0)
            {
                return OperationResult<UserResponse>.Conflict(
                    "No se puede desactivar al último " +
                    "administrador activo.");
            }
        }

        user.USU_Activo = isActive;
        user.USU_FechaModificacion = DateTime.UtcNow;
        user.USU_ModificadoPorUsuarioId = authenticatedUserId;

        // Usuarios desactivados con el esquema anterior quedaron
        // bloqueados indefinidamente; al reactivarlos se libera ese
        // bloqueo. El bloqueo temporal por intentos fallidos se respeta.
        if (isActive &&
            user.LockoutEnd.HasValue &&
            user.LockoutEnd.Value.Year >= 9999)
        {
            user.LockoutEnd = null;
        }

        // Guarda el usuario y renueva el Security Stamp en una sola
        // operación, invalidando los tokens emitidos antes del cambio.
        IdentityResult result =
            await _userManager.UpdateSecurityStampAsync(user);

        if (!result.Succeeded)
        {
            return IdentityFailure<UserResponse>(result);
        }

        return OperationResult<UserResponse>.Success(
            (await FindResponseAsync(user.Id, cancellationToken))!);
    }

    public async Task<OperationResult<UserResponse>> AssignRolesAsync(
        int id,
        AssignUserRolesRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return OperationResult<UserResponse>.Missing(
                "El usuario solicitado no existe.");
        }

        string[] requestedRoles = NormalizeRoles(request.Roles);

        string[] roleErrors =
            await ValidateRolesAsync(
                requestedRoles,
                cancellationToken);

        if (roleErrors.Length > 0)
        {
            return OperationResult<UserResponse>.Failure(roleErrors);
        }

        IList<string> currentRoles =
            await _userManager.GetRolesAsync(user);

        bool removesAdministrator =
            currentRoles.Contains(
                AdministratorRole,
                StringComparer.OrdinalIgnoreCase) &&
            !requestedRoles.Contains(
                AdministratorRole,
                StringComparer.OrdinalIgnoreCase);

        if (removesAdministrator)
        {
            if (IsPrincipalAdministrator(user))
            {
                return OperationResult<UserResponse>.Conflict(
                    "No se puede retirar el rol Administrador " +
                    "al administrador principal.");
            }

            if (await CountOtherActiveAdministratorsAsync(
                    user.Id,
                    cancellationToken) == 0)
            {
                return OperationResult<UserResponse>.Conflict(
                    "No se puede retirar el rol al último " +
                    "administrador activo.");
            }
        }

        string[] rolesToRemove = currentRoles
            .Except(requestedRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] rolesToAdd = requestedRoles
            .Except(currentRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return await _context.ExecuteInTransactionAsync(
            async token =>
            {
                user.USU_FechaModificacion = DateTime.UtcNow;
                user.USU_ModificadoPorUsuarioId = authenticatedUserId;

                IdentityResult result =
                    await _userManager.UpdateAsync(user);

                if (result.Succeeded && rolesToRemove.Length > 0)
                {
                    result = await _userManager.RemoveFromRolesAsync(
                        user,
                        rolesToRemove);
                }

                if (result.Succeeded && rolesToAdd.Length > 0)
                {
                    result = await _userManager.AddToRolesAsync(
                        user,
                        rolesToAdd);
                }

                if (!result.Succeeded)
                {
                    return IdentityFailure<UserResponse>(result);
                }

                return OperationResult<UserResponse>.Success(
                    (await FindResponseAsync(user.Id, token))!);
            },
            cancellationToken);
    }

    public async Task<OperationResult<bool>> ResetPasswordAsync(
        int id,
        ResetUserPasswordRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return OperationResult<bool>.Missing(
                "El usuario solicitado no existe.");
        }

        return await _context.ExecuteInTransactionAsync(
            async _ =>
            {
                user.USU_FechaCambioPassword = DateTime.UtcNow;
                user.USU_FechaModificacion = DateTime.UtcNow;
                user.USU_ModificadoPorUsuarioId = authenticatedUserId;

                string resetToken =
                    await _userManager
                        .GeneratePasswordResetTokenAsync(user);

                // ResetPasswordAsync valida la política, genera el
                // hash y renueva el Security Stamp.
                IdentityResult result =
                    await _userManager.ResetPasswordAsync(
                        user,
                        resetToken,
                        request.NewPassword);

                // Un restablecimiento hecho por un administrador
                // también libera el bloqueo temporal.
                if (result.Succeeded)
                {
                    result = await _userManager.SetLockoutEndDateAsync(
                        user,
                        null);
                }

                if (result.Succeeded)
                {
                    result = await _userManager
                        .ResetAccessFailedCountAsync(user);
                }

                return result.Succeeded
                    ? OperationResult<bool>.Success(true)
                    : IdentityFailure<bool>(result);
            },
            cancellationToken);
    }

    private async Task<UserResponse?> FindResponseAsync(
        int id,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user =
            await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user => user.Id == id,
                    cancellationToken);

        if (user is null)
        {
            return null;
        }

        return (await MapManyAsync([user], cancellationToken))
            .Single();
    }

    private async Task<IReadOnlyCollection<UserResponse>> MapManyAsync(
        IReadOnlyCollection<ApplicationUser> users,
        CancellationToken cancellationToken)
    {
        int[] userIds = users
            .Select(user => user.Id)
            .ToArray();

        var userRoles =
            await (from userRole in _context.UserRoles
                   join role in _context.Roles
                       on userRole.RoleId equals role.Id
                   where userIds.Contains(userRole.UserId)
                   select new { userRole.UserId, role.Name })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        ILookup<int, string> rolesByUser =
            userRoles.ToLookup(
                item => item.UserId,
                item => item.Name ?? string.Empty);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        return users
            .Select(user =>
            {
                bool isLockedOut =
                    user.LockoutEnabled &&
                    user.LockoutEnd.HasValue &&
                    user.LockoutEnd.Value > now;

                return new UserResponse
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.USU_Activo,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = isLockedOut ? user.LockoutEnd : null,
                    Roles = rolesByUser[user.Id]
                        .OrderBy(role => role)
                        .ToArray()
                };
            })
            .ToArray();
    }

    private async Task<OperationResult<UserResponse>?>
        CheckDuplicatesAsync(
            string userName,
            string email,
            int? excludingUserId)
    {
        ApplicationUser? existingByName =
            await _userManager.FindByNameAsync(userName);

        if (existingByName is not null &&
            existingByName.Id != excludingUserId)
        {
            return OperationResult<UserResponse>.Conflict(
                "El nombre de usuario ya está registrado.");
        }

        ApplicationUser? existingByEmail =
            await _userManager.FindByEmailAsync(email);

        if (existingByEmail is not null &&
            existingByEmail.Id != excludingUserId)
        {
            return OperationResult<UserResponse>.Conflict(
                "El correo electrónico ya está registrado.");
        }

        return null;
    }

    private async Task<string[]> ValidateRolesAsync(
        string[] roleNames,
        CancellationToken cancellationToken)
    {
        if (roleNames.Length == 0)
        {
            return [];
        }

        string[] normalizedNames = roleNames
            .Select(name => _userManager.NormalizeName(name)!)
            .ToArray();

        var roles =
            await _context.Roles
                .AsNoTracking()
                .Where(role =>
                    normalizedNames.Contains(role.NormalizedName!))
                .Select(role => new
                {
                    role.NormalizedName,
                    role.Name,
                    role.ROL_Activo
                })
                .ToListAsync(cancellationToken);

        List<string> errors = [];

        string[] missing = roleNames
            .Where(name => roles.All(role =>
                role.NormalizedName !=
                _userManager.NormalizeName(name)))
            .ToArray();

        if (missing.Length > 0)
        {
            errors.Add(
                "Los siguientes roles no existen: " +
                string.Join(", ", missing));
        }

        string[] inactive = roles
            .Where(role => !role.ROL_Activo)
            .Select(role => role.Name ?? string.Empty)
            .ToArray();

        if (inactive.Length > 0)
        {
            errors.Add(
                "Los siguientes roles están inactivos: " +
                string.Join(", ", inactive));
        }

        return errors.ToArray();
    }

    private Task<int> CountOtherActiveAdministratorsAsync(
        int excludingUserId,
        CancellationToken cancellationToken)
    {
        string normalizedAdministrator =
            _userManager.NormalizeName(AdministratorRole)!;

        return (from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.Id
                join user in _context.Users
                    on userRole.UserId equals user.Id
                where role.NormalizedName == normalizedAdministrator &&
                      user.USU_Activo &&
                      user.Id != excludingUserId
                select user.Id)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private static bool IsPrincipalAdministrator(
        ApplicationUser user)
    {
        return string.Equals(
            user.UserName,
            PrincipalAdministratorUserName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string[] NormalizeRoles(
        IEnumerable<string> roles)
    {
        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? NormalizeOptional(
        string? value)
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
