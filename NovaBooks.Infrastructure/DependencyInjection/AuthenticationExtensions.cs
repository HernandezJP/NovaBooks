using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NovaBooks.Application.Authentication;
using NovaBooks.Infrastructure.Services.Authentication;

namespace NovaBooks.Infrastructure;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        JwtOptions jwtOptions =
            configuration
                .GetRequiredSection(JwtOptions.SectionName)
                .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "No se encontró la configuración JWT.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
        {
            throw new InvalidOperationException(
                "No se configuró Jwt:SecretKey.");
        }

        if (jwtOptions.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey debe contener al menos 32 caracteres.");
        }

        services.Configure<JwtOptions>(
            configuration.GetRequiredSection(
                JwtOptions.SectionName));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    jwtOptions.SecretKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                        NameClaimType =
                            System.Security.Claims.ClaimTypes.Name,

                        RoleClaimType =
                            System.Security.Claims.ClaimTypes.Role
                    };
            });

        services.AddAuthorization();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        return services;
    }
}