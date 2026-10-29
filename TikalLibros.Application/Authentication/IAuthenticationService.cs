using System;
using System.Collections.Generic;
using System.Text;

namespace TikalLibros.Application.Authentication
{
    public interface IAuthenticationService
    {
        Task<LoginResult> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);
    }
}
