using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Infrastructure.Services.Authentication
{
    public sealed class JwtTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }
    }
}
