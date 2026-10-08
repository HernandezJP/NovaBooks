using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaBooks.Infrastructure.Data;
using NovaBooks.Infrastructure.Data.Identity;
using NovaBooks.Infrastructure.Data.Options;
using NovaBooks.Infrastructure.Security.Permissions;
using NovaBooks.Application.Interfaces;
using NovaBooks.Infrastructure.Services;

namespace NovaBooks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No se configuró la cadena de conexión " +
                "'DefaultConnection'. En desarrollo use " +
                "'dotnet user-secrets' en NovaBooks.Api.");
        }

        services.Configure<DatabaseOptions>(
            configuration.GetSection(
                DatabaseOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(
                        typeof(AppDbContext)
                            .Assembly
                            .FullName);

                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });
        });

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(
                options =>
                {
                    ConfigureIdentityOptions(options);
                })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "NovaBooks.Auth";

            options.Cookie.HttpOnly = true;

            options.Cookie.SecurePolicy =
                Microsoft.AspNetCore.Http
                    .CookieSecurePolicy.SameAsRequest;

            options.SlidingExpiration = true;

            options.ExpireTimeSpan =
                TimeSpan.FromHours(8);

            options.LoginPath = "/Account/Login";

            options.AccessDeniedPath =
                "/Account/AccessDenied";
        });

        services.AddAuthorization(options =>
        {
            // Todo endpoint sin [AllowAnonymous] exige autenticación.
            options.FallbackPolicy =
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
        });

        // Las políticas de permisos se crean dinámicamente
        // a partir del prefijo de HasPermissionAttribute.
        services.AddSingleton<
            IAuthorizationPolicyProvider,
            PermissionPolicyProvider>();

        services.AddSingleton<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IRoleService, RoleService>();

        services.AddScoped<
            IPermissionService,
            PermissionService>();

        services.AddScoped<ICustomerService, CustomerService>();

        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<IAuthorService, AuthorService>();

        services.AddScoped<IEditorialService, EditorialService>();

        services.AddScoped<IProductImageService, ProductImageService>();

        services.AddSingleton<IMenuService, MenuService>();

        services.AddScoped<DatabaseInitializer>();

        services.AddSingleton<DatabaseReadiness>();

        services.AddHostedService<
            DatabaseInitializationHostedService>();


        return services;
    }

    private static void ConfigureIdentityOptions(
        IdentityOptions options)
    {
        options.Password.RequiredLength = 8;

        options.Password.RequireDigit = true;

        options.Password.RequireLowercase = true;

        options.Password.RequireUppercase = true;

        options.Password.RequireNonAlphanumeric = true;

        options.Password.RequiredUniqueChars = 4;

        options.Lockout.AllowedForNewUsers = true;

        options.Lockout.MaxFailedAccessAttempts = 5;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.User.RequireUniqueEmail = true;

        options.SignIn.RequireConfirmedEmail = false;

        options.SignIn.RequireConfirmedPhoneNumber = false;
    }
}