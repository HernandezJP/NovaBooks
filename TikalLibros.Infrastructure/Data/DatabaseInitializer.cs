using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikalLibros.Infrastructure.Data.Identity;
using TikalLibros.Infrastructure.Data.Options;
using TikalLibros.Infrastructure.Data.Seed;
using TikalLibros.Infrastructure.Services.Sales;

namespace TikalLibros.Infrastructure.Data;

public sealed class DatabaseInitializer
{
    private readonly AppDbContext _context;

    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<ApplicationRole> _roleManager;

    private readonly IConfiguration _configuration;

    private readonly IHostEnvironment _environment;

    private readonly DatabaseOptions _options;

    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly DemoOperationsSeeder _demoOperations;
    private readonly DemoSalesSeeder _demoSales;
    private readonly InvoiceService _invoices;

    public DatabaseInitializer(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IHostEnvironment environment,
        IOptions<DatabaseOptions> options,
        ILogger<DatabaseInitializer> logger,
        DemoOperationsSeeder demoOperations,
        DemoSalesSeeder demoSales,
        InvoiceService invoices)
    {
        _demoOperations = demoOperations;
        _demoSales = demoSales;
        _invoices = invoices;

        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Iniciando la configuración de la base de datos.");

        await ValidateConfigurationAsync(cancellationToken);

        if (_options.ResetAndSeedOnStartup)
        {
            await ResetDatabaseAsync(cancellationToken);
        }
        else if (_options.ApplyMigrationsOnStartup)
        {
            await ApplyMigrationsAsync(cancellationToken);
        }

        if (_options.SeedOnStartup ||
            _options.ResetAndSeedOnStartup)
        {
            await SeedDatabaseAsync(cancellationToken);
        }

        _logger.LogInformation(
            "La configuración de la base de datos finalizó.");
    }

    private Task ValidateConfigurationAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_options.ResetAndSeedOnStartup &&
            !_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "ResetAndSeedOnStartup solamente puede utilizarse " +
                "en el ambiente Development.");
        }

        if (_options.ResetAndSeedOnStartup &&
            !_options.SeedOnStartup)
        {
            _logger.LogWarning(
                "ResetAndSeedOnStartup está habilitado. " +
                "El seed se ejecutará aunque SeedOnStartup sea false.");
        }

        if (!_options.ApplyMigrationsOnStartup &&
            !_options.ResetAndSeedOnStartup)
        {
            _logger.LogWarning(
                "La aplicación no aplicará migraciones " +
                "automáticamente.");
        }

        return Task.CompletedTask;
    }

    private async Task ResetDatabaseAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "ResetAndSeedOnStartup está habilitado. " +
            "Se eliminará completamente la base de datos.");

        await _context.Database.EnsureDeletedAsync(
            cancellationToken);

        _logger.LogInformation(
            "La base de datos fue eliminada.");

        await ApplyMigrationsAsync(cancellationToken);
    }

    private async Task ApplyMigrationsAsync(
        CancellationToken cancellationToken)
    {
        IEnumerable<string> pendingMigrations =
            await _context.Database
                .GetPendingMigrationsAsync(cancellationToken);

        string[] migrations = pendingMigrations.ToArray();

        if (migrations.Length == 0)
        {
            _logger.LogInformation(
                "No existen migraciones pendientes.");

            return;
        }

        _logger.LogInformation(
            "Se aplicarán {MigrationCount} migraciones: {Migrations}.",
            migrations.Length,
            string.Join(", ", migrations));

        await _context.Database.MigrateAsync(
            cancellationToken);

        _logger.LogInformation(
            "Las migraciones fueron aplicadas correctamente.");
    }

    private async Task SeedDatabaseAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ejecutando catálogos iniciales.");

        await DatabaseSeeder.SeedAsync(
            _context,
            cancellationToken);

        _logger.LogInformation(
            "Ejecutando roles, permisos y usuario administrador.");

        await IdentitySeeder.SeedAsync(
            _userManager,
            _roleManager,
            _configuration);

        await OrganizationSeeder.SeedAdministratorEmployeeAsync(
            _context,
            _userManager,
            _configuration,
            cancellationToken);

        if (_options.SeedDemoData ?? _environment.IsDevelopment())
        {
            _logger.LogInformation(
                "Cargando datos de demostración del catálogo.");

            await DemoCatalogSeeder.SeedAsync(
                _context,
                Services.ProductImageStorage.GetDirectory(_configuration, _environment),
                cancellationToken);

            _logger.LogInformation(
                "Cargando datos de demostración de compras e inventario.");

            await _demoOperations.SeedAsync(cancellationToken);

            _logger.LogInformation(
                "Cargando datos de demostración de pedidos, ventas y caja.");

            await _demoSales.SeedAsync(cancellationToken);
        }

        // Ventas registradas antes de la facturación electrónica.
        int invoices = await _invoices.IssueMissingAsync(cancellationToken);

        if (invoices > 0)
        {
            _logger.LogInformation("Se emitieron {Count} factura(s) de ventas anteriores.", invoices);
        }


        _logger.LogInformation(
            "Los datos iniciales fueron creados correctamente.");
    }
}