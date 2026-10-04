using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaBooks.Infrastructure.Data.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NovaBooks.Infrastructure.Services.Authentication
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _jwtOptions;

        public JwtTokenService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public JwtTokenResult GenerateToken(
            ApplicationUser user,
            IEnumerable<string> roles,
            IEnumerable<string> permissions)
        {
            ArgumentNullException.ThrowIfNull(user);

            ValidateOptions();

            DateTime issuedAtUtc = DateTime.UtcNow;
            DateTime expiresAtUtc =
                issuedAtUtc.AddMinutes(_jwtOptions.ExpirationMinutes);

            List<Claim> claims =
            [
                new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.UserName ?? string.Empty),

            new Claim(
                JwtRegisteredClaimNames.UniqueName,
                user.UserName ?? string.Empty),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty),

            new Claim(
                ClaimTypes.Email,
                user.Email ?? string.Empty),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new Claim(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(issuedAtUtc)
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
            ];

            foreach (string role in roles
                         .Where(role => !string.IsNullOrWhiteSpace(role))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach (string permission in permissions
                         .Where(permission =>
                             !string.IsNullOrWhiteSpace(permission))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("permission", permission));
            }

            SymmetricSecurityKey securityKey = new(
                Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

            SigningCredentials signingCredentials = new(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: issuedAtUtc,
                expires: expiresAtUtc,
                signingCredentials: signingCredentials);

            string accessToken =
                new JwtSecurityTokenHandler().WriteToken(token);

            return new JwtTokenResult
            {
                AccessToken = accessToken,
                ExpiresAtUtc = expiresAtUtc
            };
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_jwtOptions.SecretKey))
            {
                throw new InvalidOperationException(
                    "No se configuró Jwt:SecretKey.");
            }

            if (_jwtOptions.SecretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:SecretKey debe contener al menos 32 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
            {
                throw new InvalidOperationException(
                    "No se configuró Jwt:Issuer.");
            }

            if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
            {
                throw new InvalidOperationException(
                    "No se configuró Jwt:Audience.");
            }

            if (_jwtOptions.ExpirationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Jwt:ExpirationMinutes debe ser mayor que cero.");
            }

        }
    }
}
