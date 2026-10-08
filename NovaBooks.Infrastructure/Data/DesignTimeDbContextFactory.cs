using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NovaBooks.Infrastructure.Data;

/// <summary>
/// Usado solo por las herramientas de EF Core (dotnet ef). Toma la cadena
/// de conexión de NovaBooks.Api (appsettings, user-secrets o variable de
/// entorno ConnectionStrings__DefaultConnection). Generar migraciones o
/// revisar cambios pendientes no se conecta a la base.
/// </summary>
public sealed class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ApiUserSecretsId =
        "041d075a-f245-4514-9ede-d3651629291d";

    public AppDbContext CreateDbContext(string[] args)
    {
        string apiDirectory = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "NovaBooks.Api"));

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(ApiUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        string connectionString =
            configuration.GetConnectionString("DefaultConnection") is { Length: > 0 } value
                ? value
                : "Server=(localdb)\\MSSQLLocalDB;Database=NovaBooksDesign;Trusted_Connection=True";

        DbContextOptions<AppDbContext> options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(
                    connectionString,
                    sqlServer => sqlServer.MigrationsAssembly(
                        typeof(AppDbContext).Assembly.FullName))
                .Options;

        return new AppDbContext(options);
    }
}
