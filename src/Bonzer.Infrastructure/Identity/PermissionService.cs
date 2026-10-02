using Bonzer.Application.Common.Caching;
using Bonzer.Application.Security;

namespace Bonzer.Infrastructure.Identity;

public sealed class PermissionService(BonzerDbContext db, ICacheService cache) : IPermissionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(Guid? tenantId, IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var permissions = await cache.GetOrCreateAsync(
                CacheKeys.RolePermissions(tenantId, role),
                ct => LoadAsync(tenantId, role, ct),
                CacheTtl, cancellationToken);
            result.UnionWith(permissions);
        }
        return result;
    }

    public Task InvalidateRoleAsync(Guid? tenantId, string role, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(CacheKeys.RolePermissions(tenantId, role), cancellationToken);

    private async Task<string[]> LoadAsync(Guid? tenantId, string role, CancellationToken cancellationToken)
    {
        var normalized = role.ToUpperInvariant();

        // A tenant's own role overrides the system role with the same name.
        var roleId = await db.Roles
            .Where(r => r.NormalizedName == normalized && (r.TenantId == null || r.TenantId == tenantId))
            .OrderBy(r => r.TenantId == null)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleId is null) return [];

        return await db.RoleClaims
            .Where(c => c.RoleId == roleId && c.ClaimType == AppClaimTypes.Permission && c.ClaimValue != null)
            .Select(c => c.ClaimValue!)
            .ToArrayAsync(cancellationToken);
    }
}
