using TikalLibros.Infrastructure.Data.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace TikalLibros.Infrastructure.Services.Authentication
{
    public interface IJwtTokenService
    {
        JwtTokenResult GenerateToken(
            ApplicationUser user,
            IEnumerable<string> roles,
            IEnumerable<string> permissions);
    }
}
