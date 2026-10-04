using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Application.Authentication
{
    public sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public DateTime ExpiresAtUtc { get; set; }

        public AuthenticatedUserResponse User { get; set; } = new();
    }
}
