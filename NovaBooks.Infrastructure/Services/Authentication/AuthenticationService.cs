using Microsoft.AspNetCore.Identity;
using NovaBooks.Application.Authentication;
using NovaBooks.Infrastructure.Data.Identity;

namespace NovaBooks.Infrastructure.Services.Authentication
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserAccessService _userAccessService;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserAccessService userAccessService,
            IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userAccessService = userAccessService;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<LoginResult> LoginAsync(
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
                return LoginResult.InvalidCredentials();
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return LoginResult.LockedOut(user.LockoutEnd);
            }

            SignInResult signInResult =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                return LoginResult.LockedOut(user.LockoutEnd);
            }

            if (!signInResult.Succeeded)
            {
                return LoginResult.InvalidCredentials();
            }

            // El estado desactivado solo se revela con la
            // contraseña correcta para no exponer cuentas.
            if (!user.USU_Activo)
            {
                return LoginResult.Disabled();
            }

            if (string.IsNullOrEmpty(user.SecurityStamp))
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            UserAccessSnapshot? access =
                await _userAccessService.GetAccessAsync(
                    user.Id,
                    cancellationToken);

            if (access is null)
            {
                return LoginResult.InvalidCredentials();
            }

            user.USU_FechaUltimoAcceso = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            JwtTokenResult tokenResult =
                _jwtTokenService.GenerateToken(
                    user,
                    access.Roles,
                    access.Permissions);

            return LoginResult.Success(new LoginResponse
            {
                AccessToken = tokenResult.AccessToken,
                TokenType = "Bearer",
                ExpiresAtUtc = tokenResult.ExpiresAtUtc,

                User = new AuthenticatedUserResponse
                {
                    Id = user.Id.ToString(),
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = access.Roles,
                    Permissions = access.Permissions
                }
            });
        }
    }
}
