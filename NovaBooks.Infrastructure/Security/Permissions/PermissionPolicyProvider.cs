using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NovaBooks.Infrastructure.Security.Permissions;

public sealed class PermissionPolicyProvider
    : IAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "Permission:";

    private readonly DefaultAuthorizationPolicyProvider
        _defaultPolicyProvider;

    public PermissionPolicyProvider(
        IOptions<AuthorizationOptions> options)
    {
        _defaultPolicyProvider =
            new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?>
        GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(
                PolicyPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return _defaultPolicyProvider
                .GetPolicyAsync(policyName);
        }

        string permission =
            policyName[PolicyPrefix.Length..];

        if (string.IsNullOrWhiteSpace(permission))
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        AuthorizationPolicy policy =
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(
                    new PermissionRequirement(permission))
                .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy>
        GetDefaultPolicyAsync()
    {
        return _defaultPolicyProvider
            .GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?>
        GetFallbackPolicyAsync()
    {
        return _defaultPolicyProvider
            .GetFallbackPolicyAsync();
    }
}