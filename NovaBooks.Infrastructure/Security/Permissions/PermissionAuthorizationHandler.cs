using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace NovaBooks.Infrastructure.Security.Permissions
{
    public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            bool hasPermission = context.User.Claims.Any(claim =>
                claim.Type == CustomClaimTypes.Permission &&
                claim.Value == requirement.Permission);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
