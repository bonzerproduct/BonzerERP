using Bonzer.Application.Common.Interfaces;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Bonzer.Api.Authorization;

/// <summary>
/// [HasPermission(Permissions.Students.View)] on a controller or action.
/// Creates a policy "perm:students.view" on demand, so no policy has to be registered by hand.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(PolicyPrefix + permission)
{
    public const string PolicyPrefix = "perm:";
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
            return await base.GetPolicyAsync(policyName);

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[HasPermissionAttribute.PolicyPrefix.Length..]))
            .Build();
    }
}

/// <summary>Checks the permission against the user's roles (resolved from the DB, cached in Redis).</summary>
public sealed class PermissionHandler(ICurrentUser currentUser, IPermissionService permissions)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (currentUser.IsPlatformAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        var granted = await permissions.GetPermissionsAsync(currentUser.TenantId, currentUser.Roles);
        if (granted.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
