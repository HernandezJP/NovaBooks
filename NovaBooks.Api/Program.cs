using Microsoft.OpenApi;
using NovaBooks.Infrastructure;
using NovaBooks.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "NovaBooks API",
            Version = "v1",
            Description =
                "API del sistema administrativo NovaBooks."
        });

    options.AddSecurityDefinition(
        "bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Ingrese el token JWT obtenido en /api/auth/login."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "bearer",
                    document)
            ] = []
        });
});

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddJwtAuthentication(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "NovaBooks API v1");

        options.DocumentTitle =
            "NovaBooks API";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await using (AsyncServiceScope scope =
    app.Services.CreateAsyncScope())
{
    DatabaseInitializer initializer =
        scope.ServiceProvider
            .GetRequiredService<DatabaseInitializer>();

    await initializer.InitializeAsync();
}

app.Run();