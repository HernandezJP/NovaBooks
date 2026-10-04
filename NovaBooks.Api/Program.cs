using NovaBooks.Infrastructure;
using NovaBooks.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Controladores de la API
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Base de datos, Identity, permisos e inicializador
builder.Services.AddInfrastructure(
    builder.Configuration);

var app = builder.Build();

// OpenAPI solamente durante desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Migraciones, reinicio opcional y datos iniciales
await using (AsyncServiceScope scope =
    app.Services.CreateAsyncScope())
{
    DatabaseInitializer initializer =
        scope.ServiceProvider
            .GetRequiredService<DatabaseInitializer>();

    await initializer.InitializeAsync();
}

app.Run();