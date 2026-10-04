using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaBooks.Infrastructure.Data;
using NovaBooks.Infrastructure.Data.Identity;
using NovaBooks.Infrastructure.Data.Options;
using NovaBooks.Infrastructure.Security.Permissions;

namespace NovaBooks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString(
                "DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión " +
                "'DefaultConnection'.");

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
            foreach (string permission in
                     SystemPermissions.GetAll())
            {
                options.AddPolicy(
                    permission,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            new PermissionRequirement(
                                permission));
                    });
            }
        });

        services.AddSingleton<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        services.AddScoped<DatabaseInitializer>();

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