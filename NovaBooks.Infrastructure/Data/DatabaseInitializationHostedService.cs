using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NovaBooks.Infrastructure.Data;

/// <summary>
/// Indica si la base de datos terminó de inicializarse.
/// Mientras no esté lista, la API responde 503.
/// </summary>
public sealed class DatabaseReadiness
{
    private volatile bool _isReady;

    public bool IsReady => _isReady;

    public void MarkReady()
    {
        _isReady = true;
    }
}

/// <summary>
/// Ejecuta DatabaseInitializer en segundo plano para que la API
/// pueda responder "sistema iniciando" en lugar de rechazar
/// conexiones mientras se aplican migraciones y seeds.
/// </summary>
public sealed class DatabaseInitializationHostedService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly DatabaseReadiness _readiness;

    private readonly IHostApplicationLifetime _lifetime;

    private readonly ILogger<DatabaseInitializationHostedService>
        _logger;

    public DatabaseInitializationHostedService(
        IServiceScopeFactory scopeFactory,
        DatabaseReadiness readiness,
        IHostApplicationLifetime lifetime,
        ILogger<DatabaseInitializationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _readiness = readiness;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope =
                _scopeFactory.CreateAsyncScope();

            DatabaseInitializer initializer =
                scope.ServiceProvider
                    .GetRequiredService<DatabaseInitializer>();

            await initializer.InitializeAsync(stoppingToken);

            _readiness.MarkReady();
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogCritical(
                exception,
                "No fue posible inicializar la base de datos. " +
                "La API se detendrá.");

            _lifetime.StopApplication();
        }
    }
}
