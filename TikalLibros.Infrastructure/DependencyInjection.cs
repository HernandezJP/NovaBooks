using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TikalLibros.Infrastructure.Data;
using TikalLibros.Infrastructure.Data.Identity;
using TikalLibros.Infrastructure.Data.Options;
using TikalLibros.Infrastructure.Security.Permissions;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Services;
using TikalLibros.Infrastructure.Services.People;
using TikalLibros.Infrastructure.Services.Documents;
using TikalLibros.Infrastructure.Services.Purchasing;
using TikalLibros.Infrastructure.Services.Inventory;
using TikalLibros.Infrastructure.Services.Audit;
using TikalLibros.Infrastructure.Services.Cash;
using TikalLibros.Infrastructure.Services.Sales;
using TikalLibros.Infrastructure.Services.Reports;
using TikalLibros.Infrastructure.Services.Configuration;
using TikalLibros.Infrastructure.Services.Organization;
using TikalLibros.Infrastructure.Data.Seed;




namespace TikalLibros.Infrastructure;

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
                "'dotnet user-secrets' en TikalLibros.Api.");
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
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "TikalLibros.Auth";

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

        services.AddScoped<PersonContactWriter>();

        services.AddScoped<ICustomerService, CustomerService>();

        services.AddScoped<IEmployeeService, EmployeeService>();

        services.AddScoped<ISupplierService, SupplierService>();

        services.AddScoped<DocumentNumberService>();

        services.AddScoped<CurrentEmployeeResolver>();

        services.AddHttpContextAccessor();

        services.AddScoped<AuditTrail>();

        services.AddScoped<ISystemParameterService, SystemParameterService>();

        services.AddScoped<CashService>();

        services.AddScoped<ICashService>(provider => provider.GetRequiredService<CashService>());

        services.AddScoped<OrderService>();

        services.AddScoped<IOrderService>(provider => provider.GetRequiredService<OrderService>());

        services.AddScoped<InvoiceService>();

        services.AddScoped<ISaleService, SaleService>();

        services.AddScoped<IReturnService, ReturnService>();

        services.AddScoped<IAuditService, AuditService>();

        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<PurchaseOrderService>();

        services.AddScoped<IPurchaseOrderService>(provider => provider.GetRequiredService<PurchaseOrderService>());

        services.AddScoped<IReceptionService, ReceptionService>();

        services.AddScoped<InventoryService>();

        services.AddScoped<IInventoryService>(provider => provider.GetRequiredService<InventoryService>());

        services.AddScoped<ITransferService, TransferService>();

        services.AddScoped<DemoOperationsSeeder>();

        services.AddScoped<DemoSalesSeeder>();




        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<IAuthorService, AuthorService>();

        services.AddScoped<IEditorialService, EditorialService>();

        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IPriceListService, PriceListService>();

        services.AddScoped<IBranchService, BranchService>();

        services.AddScoped<IWarehouseService, WarehouseService>();

        services.AddScoped<ICashRegisterService, CashRegisterService>();

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