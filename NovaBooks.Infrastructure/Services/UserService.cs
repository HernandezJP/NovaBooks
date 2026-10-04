using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NovaBooks.Application.Common;
using NovaBooks.Application.DTOs.Users;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Data.Identity;

namespace NovaBooks.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private const string AdministratorRole = "Administrador";

    private const string PrincipalAdministratorUserName =
        "admin";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<PagedResponse<UserResponse>> GetPagedAsync(
        string? search,
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
            _userManager.Users.AsNoTracking();

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

        int totalItems =
            await query.CountAsync(cancellationToken);

        List<ApplicationUser> users =
            await query
                .OrderBy(user => user.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        List<UserResponse> responses = [];

        foreach (ApplicationUser user in users)
        {
            responses.Add(await MapAsync(user));
        }

        return new PagedResponse<UserResponse>
        {
            Items = responses,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                id.ToString());

        if (user is null)
        {
            return null;
        }

        return await MapAsync(user);
    }

    public async Task<UserOperationResult<UserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string userName = request.UserName.Trim();
        string email = request.Email.Trim();

        ApplicationUser? existingByName =
            await _userManager.FindByNameAsync(userName);

        if (existingByName is not null)
        {
            return UserOperationResult<UserResponse>.Failure(
                "El nombre de usuario ya está registrado.");
        }

        ApplicationUser? existingByEmail =
            await _userManager.FindByEmailAsync(email);

        if (existingByEmail is not null)
        {
            return UserOperationResult<UserResponse>.Failure(
                "El correo electrónico ya está registrado.");
        }

        string[] requestedRoles = request.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] invalidRoles =
            await GetInvalidRolesAsync(requestedRoles);

        if (invalidRoles.Length > 0)
        {
            return UserOperationResult<UserResponse>.Failure(
                "Los siguientes roles no existen: " +
                string.Join(", ", invalidRoles));
        }

        ApplicationUser user = new()
        {
            UserName = userName,
            Email = email,
            PhoneNumber =
                NormalizeOptional(request.PhoneNumber),
            EmailConfirmed = true,
            LockoutEnabled = true,
            LockoutEnd = null
        };

        IdentityResult createResult =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!createResult.Succeeded)
        {
            return IdentityFailure<UserResponse>(
                createResult);
        }

        if (requestedRoles.Length > 0)
        {
            IdentityResult rolesResult =
                await _userManager.AddToRolesAsync(
                    user,
                    requestedRoles);

            if (!rolesResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return IdentityFailure<UserResponse>(
                    rolesResult);
            }
        }

        return UserOperationResult<UserResponse>.Success(
            await MapAsync(user));
    }

    public async Task<UserOperationResult<UserResponse>> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                id.ToString());

        if (user is null)
        {
            return UserOperationResult<UserResponse>.Missing(
                "El usuario solicitado no existe.");
        }

        string newUserName =
            request.UserName.Trim();

        string newEmail =
            request.Email.Trim();

        ApplicationUser? existingByName =
            await _userManager.FindByNameAsync(
                newUserName);

        if (existingByName is not null &&
            existingByName.Id != user.Id)
        {
            return UserOperationResult<UserResponse>.Failure(
                "El nombre de usuario ya está registrado.");
        }

        ApplicationUser? existingByEmail =
            await _userManager.FindByEmailAsync(
                newEmail);

        if (existingByEmail is not null &&
            existingByEmail.Id != user.Id)
        {
            return UserOperationResult<UserResponse>.Failure(
                "El correo electrónico ya está registrado.");
        }

        user.UserName = newUserName;
        user.Email = newEmail;
        user.PhoneNumber =
            NormalizeOptional(request.PhoneNumber);

        IdentityResult result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return IdentityFailure<UserResponse>(result);
        }

        return UserOperationResult<UserResponse>.Success(
            await MapAsync(user));
    }

    public async Task<UserOperationResult<UserResponse>>
        ChangeStatusAsync(
            int id,
            bool isActive,
            int authenticatedUserId,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                id.ToString());

        if (user is null)
        {
            return UserOperationResult<UserResponse>.Missing(
                "El usuario solicitado no existe.");
        }

        if (!isActive && id == authenticatedUserId)
        {
            return UserOperationResult<UserResponse>.Failure(
                "No puede desactivar su propio usuario.");
        }

        if (!isActive &&
            string.Equals(
                user.UserName,
                PrincipalAdministratorUserName,
                StringComparison.OrdinalIgnoreCase))
        {
            return UserOperationResult<UserResponse>.Failure(
                "El administrador principal no puede ser desactivado.");
        }

        if (!isActive &&
            await _userManager.IsInRoleAsync(
                user,
                AdministratorRole))
        {
            IList<ApplicationUser> administrators =
                await _userManager.GetUsersInRoleAsync(
                    AdministratorRole);

            int activeAdministrators =
                administrators.Count(IsActive);

            if (activeAdministrators <= 1)
            {
                return UserOperationResult<UserResponse>.Failure(
                    "No se puede desactivar al último " +
                    "administrador activo.");
            }
        }

        IdentityResult result;

        if (isActive)
        {
            result =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    null);

            if (result.Succeeded)
            {
                result =
                    await _userManager
                        .ResetAccessFailedCountAsync(user);
            }
        }
        else
        {
            user.LockoutEnabled = true;

            IdentityResult updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return IdentityFailure<UserResponse>(
                    updateResult);
            }

            result =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.MaxValue);
        }

        if (!result.Succeeded)
        {
            return IdentityFailure<UserResponse>(result);
        }

        return UserOperationResult<UserResponse>.Success(
            await MapAsync(user));
    }

    public async Task<UserOperationResult<UserResponse>>
        AssignRolesAsync(
            int id,
            AssignUserRolesRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                id.ToString());

        if (user is null)
        {
            return UserOperationResult<UserResponse>.Missing(
                "El usuario solicitado no existe.");
        }

        string[] requestedRoles = request.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] invalidRoles =
            await GetInvalidRolesAsync(requestedRoles);

        if (invalidRoles.Length > 0)
        {
            return UserOperationResult<UserResponse>.Failure(
                "Los siguientes roles no existen: " +
                string.Join(", ", invalidRoles));
        }

        IList<string> currentRoles =
            await _userManager.GetRolesAsync(user);

        bool currentlyAdministrator =
            currentRoles.Contains(
                AdministratorRole,
                StringComparer.OrdinalIgnoreCase);

        bool willBeAdministrator =
            requestedRoles.Contains(
                AdministratorRole,
                StringComparer.OrdinalIgnoreCase);

        if (currentlyAdministrator &&
            !willBeAdministrator)
        {
            if (string.Equals(
                    user.UserName,
                    PrincipalAdministratorUserName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return UserOperationResult<UserResponse>.Failure(
                    "No se puede retirar el rol Administrador " +
                    "al administrador principal.");
            }

            IList<ApplicationUser> administrators =
                await _userManager.GetUsersInRoleAsync(
                    AdministratorRole);

            if (administrators.Count <= 1)
            {
                return UserOperationResult<UserResponse>.Failure(
                    "No se puede retirar el rol al último " +
                    "administrador.");
            }
        }

        string[] rolesToRemove = currentRoles
            .Except(
                requestedRoles,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] rolesToAdd = requestedRoles
            .Except(
                currentRoles,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            IdentityResult removeResult =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    rolesToRemove);

            if (!removeResult.Succeeded)
            {
                return IdentityFailure<UserResponse>(
                    removeResult);
            }
        }

        if (rolesToAdd.Length > 0)
        {
            IdentityResult addResult =
                await _userManager.AddToRolesAsync(
                    user,
                    rolesToAdd);

            if (!addResult.Succeeded)
            {
                if (rolesToRemove.Length > 0)
                {
                    await _userManager.AddToRolesAsync(
                        user,
                        rolesToRemove);
                }

                return IdentityFailure<UserResponse>(
                    addResult);
            }
        }

        return UserOperationResult<UserResponse>.Success(
            await MapAsync(user));
    }

    public async Task<UserOperationResult<bool>>
        ResetPasswordAsync(
            int id,
            ResetUserPasswordRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser? user =
            await _userManager.FindByIdAsync(
                id.ToString());

        if (user is null)
        {
            return UserOperationResult<bool>.Missing(
                "El usuario solicitado no existe.");
        }

        string resetToken =
            await _userManager
                .GeneratePasswordResetTokenAsync(user);

        IdentityResult resetResult =
            await _userManager.ResetPasswordAsync(
                user,
                resetToken,
                request.NewPassword);

        if (!resetResult.Succeeded)
        {
            return IdentityFailure<bool>(resetResult);
        }

        IdentityResult stampResult =
            await _userManager
                .UpdateSecurityStampAsync(user);

        if (!stampResult.Succeeded)
        {
            return IdentityFailure<bool>(stampResult);
        }

        return UserOperationResult<bool>.Success(true);
    }

    private async Task<UserResponse> MapAsync(
        ApplicationUser user)
    {
        IList<string> roles =
            await _userManager.GetRolesAsync(user);

        bool isLockedOut =
            user.LockoutEnd.HasValue &&
            user.LockoutEnd.Value >
            DateTimeOffset.UtcNow;

        return new UserResponse
        {
            Id = user.Id,
            UserName =
                user.UserName ?? string.Empty,
            Email =
                user.Email ?? string.Empty,
            PhoneNumber =
                user.PhoneNumber,
            IsActive =
                !isLockedOut,
            IsLockedOut =
                isLockedOut,
            LockoutEnd =
                user.LockoutEnd,
            Roles = roles
                .OrderBy(role => role)
                .ToArray()
        };
    }

    private async Task<string[]> GetInvalidRolesAsync(
        IEnumerable<string> roleNames)
    {
        List<string> invalidRoles = [];

        foreach (string roleName in roleNames)
        {
            if (!await _roleManager
                    .RoleExistsAsync(roleName))
            {
                invalidRoles.Add(roleName);
            }
        }

        return invalidRoles.ToArray();
    }

    private static bool IsActive(
        ApplicationUser user)
    {
        return !user.LockoutEnd.HasValue ||
               user.LockoutEnd.Value <=
               DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static UserOperationResult<T>
        IdentityFailure<T>(
            IdentityResult identityResult)
    {
        string[] errors = identityResult.Errors
            .Select(error => error.Description)
            .Distinct()
            .ToArray();

        return UserOperationResult<T>.Failure(errors);
    }
}