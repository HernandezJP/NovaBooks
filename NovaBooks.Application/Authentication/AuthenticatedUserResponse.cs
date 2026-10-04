using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBooks.Application.Authentication
{
    public sealed class AuthenticatedUserResponse
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public IReadOnlyCollection<string> Roles { get; set; } =
            Array.Empty<string>();

        public IReadOnlyCollection<string> Permissions { get; set; } =
            Array.Empty<string>();
    }
}
