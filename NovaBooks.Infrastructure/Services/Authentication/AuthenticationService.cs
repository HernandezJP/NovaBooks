using Microsoft.AspNetCore.Identity;
using NovaBooks.Application.Authentication;
using NovaBooks.Infrastructure.Data.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace NovaBooks.Infrastructure.Services.Authentication
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string userNameOrEmail = request.UserNameOrEmail.Trim();

            ApplicationUser? user =
                await _userManager.FindByNameAsync(userNameOrEmail);

            user ??=
                await _userManager.FindByEmailAsync(userNameOrEmail);

            if (user is null)
            {
                return null;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return null;
            }

            SignInResult signInResult =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                return null;
            }

            IList<string> userRoles =
                await _userManager.GetRolesAsync(user);

            HashSet<string> permissions =
                new(StringComparer.OrdinalIgnoreCase);

            IList<Claim> userClaims =
                await _userManager.GetClaimsAsync(user);

            foreach (Claim claim in userClaims
                         .Where(claim =>
                             claim.Type.Equals(
                                 "permission",
                                 StringComparison.OrdinalIgnoreCase)))
            {
                permissions.Add(claim.Value);
            }

            foreach (string roleName in userRoles)
            {
                ApplicationRole? role =
                    await _roleManager.FindByNameAsync(roleName);

                if (role is null)
                {
                    continue;
                }

                IList<Claim> roleClaims =
                    await _roleManager.GetClaimsAsync(role);

                foreach (Claim claim in roleClaims
                             .Where(claim =>
                                 claim.Type.Equals(
                                     "permission",
                                     StringComparison.OrdinalIgnoreCase)))
                {
                    permissions.Add(claim.Value);
                }
            }

            JwtTokenResult tokenResult =
                _jwtTokenService.GenerateToken(
                    user,
                    userRoles,
                    permissions);

            return new LoginResponse
            {
                AccessToken = tokenResult.AccessToken,
                TokenType = "Bearer",
                ExpiresAtUtc = tokenResult.ExpiresAtUtc,

                User = new AuthenticatedUserResponse
                {
                    Id = user.Id.ToString(),
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = userRoles.ToArray(),
                    Permissions = permissions
                        .OrderBy(permission => permission)
                        .ToArray()
                }
            };
        }
    }

}