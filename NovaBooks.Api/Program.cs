using Microsoft.OpenApi;
using NovaBooks.Api.ErrorHandling;
using NovaBooks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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

app.UseExceptionHandler();

app.UseStatusCodePages();

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

app.UseMiddleware<DatabaseReadinessMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;