using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NovaBooks.Infrastructure.Data;
using NovaBooks.Infrastructure.Data.Identity;
using NovaBooks.Infrastructure.Data.Seed;

namespace NovaBooks.IntegrationTests.Infrastructure;

/// <summary>
/// Levanta NovaBooks.Api completa en memoria (TestHost) con una base SQLite
/// propia, catálogos iniciales, roles y el usuario administrador. Cada clase
/// de pruebas recibe una instancia independiente.
/// </summary>
public class NovaBooksApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminUser = "admin";
    public const string AdminPassword = "Admin123*Pruebas";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public string ImagesPath { get; } =
        Path.Combine(Path.GetTempPath(), "novabooks-tests", Guid.NewGuid().ToString("N"));

    /// <summary>Si es false, la base no se marca como lista (pruebas de 503).</summary>
    protected virtual bool MarkDatabaseReady => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting aplica antes de que Program lea la configuración.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
        builder.UseSetting("IdentitySeed:Administrator:UserName", AdminUser);
        builder.UseSetting("IdentitySeed:Administrator:Email", "admin@novabooks.test");
        builder.UseSetting("IdentitySeed:Administrator:Password", AdminPassword);
        builder.UseSetting("Jwt:Issuer", "NovaBooks.Api");
        builder.UseSetting("Jwt:Audience", "NovaBooks.Client");
        builder.UseSetting("Jwt:SecretKey", "clave-de-pruebas-de-integracion-novabooks-2026");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.UseSetting("Storage:ProductImagesPath", ImagesPath);
        builder.UseSetting("Database:SeedDemoData", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_connection)
                .ReplaceService<IModelCustomizer, SqliteModelCustomizer>());

            // El esquema y los datos iniciales se crean en InitializeAsync.
            ServiceDescriptor? initializer = services.FirstOrDefault(descriptor =>
                descriptor.ImplementationType == typeof(DatabaseInitializationHostedService));

            if (initializer is not null)
            {
                services.Remove(initializer);
            }
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        using IServiceScope scope = Services.CreateScope();
        IServiceProvider provider = scope.ServiceProvider;

        AppDbContext context = provider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        await DatabaseSeeder.SeedAsync(context);

        await IdentitySeeder.SeedAsync(
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<RoleManager<ApplicationRole>>(),
            provider.GetRequiredService<IConfiguration>());

        if (MarkDatabaseReady)
        {
            provider.GetRequiredService<DatabaseReadiness>().MarkReady();
        }
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();

        if (Directory.Exists(ImagesPath))
        {
            Directory.Delete(ImagesPath, recursive: true);
        }

        await base.DisposeAsync();
    }

    /// <summary>Ejecuta una consulta directa contra la base de pruebas.</summary>
    public async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using IServiceScope scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

/// <summary>API cuya base nunca se marca como lista (pruebas de 503).</summary>
public sealed class NotReadyApiFactory : NovaBooksApiFactory
{
    protected override bool MarkDatabaseReady => false;
}
