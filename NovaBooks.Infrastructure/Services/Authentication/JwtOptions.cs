using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Services.Authentication
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public int ExpirationMinutes { get; set; } = 60;
    }
}
