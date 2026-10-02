namespace Bonzer.Application.Common.Caching;

/// <summary>All cache keys in one place so invalidation is easy to find.</summary>
public static class CacheKeys
{
    public static string AcademicYears(Guid branchId) => $"academic-years:{branchId:N}";

    public static string Lookups(Guid tenantId, string category) => $"lookups:{tenantId:N}:{category}";

    public static string RolePermissions(Guid? tenantId, string role) =>
        $"perm:{(tenantId.HasValue ? tenantId.Value.ToString("N") : "system")}:{role.ToUpperInvariant()}";
}
