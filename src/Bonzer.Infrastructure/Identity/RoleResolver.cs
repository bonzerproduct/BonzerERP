using Bonzer.Application.Security;

namespace Bonzer.Infrastructure.Identity;

/// <summary>
/// Resolves role names for a tenant (a tenant's own role overrides the built-in one with the same name)
/// and checks that the caller may grant them. Shared by user and role administration.
/// </summary>
public sealed class RoleResolver(BonzerDbContext db, ICurrentUser currentUser, IPermissionService permissions)
{
    public async Task<IReadOnlyList<ApplicationRole>> ResolveAsync(Guid? tenantId, IEnumerable<string> roleNames,
        CancellationToken cancellationToken)
    {
        var resolved = new List<ApplicationRole>();
        foreach (var name in roleNames.Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var normalized = name.ToUpperInvariant();
            var role = await db.Roles
                           .Where(r => r.NormalizedName == normalized && (r.TenantId == null || r.TenantId == tenantId))
                           .OrderBy(r => r.TenantId == null)
                           .FirstOrDefaultAsync(cancellationToken)
                       ?? throw new BusinessRuleException($"Role '{name}' does not exist.");
            resolved.Add(role);
        }
        return resolved;
    }

    /// <summary>
    /// Prevents privilege escalation: SuperAdmin is platform-only, and anyone below TenantAdmin can only
    /// grant permissions they already hold themselves.
    /// </summary>
    public async Task EnsureCanGrantAsync(IEnumerable<string> grantedPermissions, IEnumerable<string> roleNames,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsSystem || currentUser.IsPlatformAdmin) return;

        var names = roleNames.ToList();
        if (names.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
            throw new ForbiddenAccessException("Only the platform can grant the SuperAdmin role.");

        var granted = grantedPermissions.ToList();
        if (granted.Any(Permissions.IsPlatformOnly))
            throw new ForbiddenAccessException("Platform permissions cannot be granted inside an institution.");

        if (currentUser.Roles.Any(AppRoles.FullTenantControl.Contains)) return;

        // Tenant-wide roles (TenantAdmin, Director) are matched by name, so they are reserved as well:
        // otherwise a custom role called "Director" would silently grant access to every branch.
        var reserved = names.Where(AppRoles.TenantWide.Contains).ToList();
        if (reserved.Count > 0)
            throw new ForbiddenAccessException($"Only an institution administrator can grant or define: {string.Join(", ", reserved)}.");

        var mine = await permissions.GetPermissionsAsync(currentUser.TenantId, currentUser.Roles, cancellationToken);
        var missing = granted.Where(p => !mine.Contains(p)).Distinct().ToList();
        if (missing.Count > 0)
            throw new ForbiddenAccessException($"You cannot grant permissions you do not have: {string.Join(", ", missing)}.");
    }

    public async Task<IReadOnlyList<string>> GetPermissionsOfRolesAsync(Guid? tenantId, IEnumerable<string> roleNames,
        CancellationToken cancellationToken) =>
        (await permissions.GetPermissionsAsync(tenantId, roleNames, cancellationToken)).ToList();
}
