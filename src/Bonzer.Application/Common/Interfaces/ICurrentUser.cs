namespace Bonzer.Application.Common.Interfaces;

/// <summary>The caller of the current request, read from the JWT.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>True for background jobs, migrations and seeding (no HTTP request). Bypasses tenant filters.</summary>
    bool IsSystem { get; }

    Guid? UserId { get; }
    string? UserName { get; }
    Guid? TenantId { get; }
    UserType? UserType { get; }

    /// <summary>Branches this user may work in (empty for tenant-wide admins).</summary>
    IReadOnlyList<Guid> BranchIds { get; }
    IReadOnlyList<string> Roles { get; }

    Guid? StaffId { get; }
    Guid? GuardianId { get; }
    Guid? StudentId { get; }

    bool IsPlatformAdmin { get; }

    /// <summary>Tenant-wide admins (TenantAdmin) can access every branch of their tenant.</summary>
    bool HasTenantWideAccess { get; }

    string? IpAddress { get; }
    string? CorrelationId { get; }

    bool IsInRole(string role);
}

public static class CurrentUserExtensions
{
    public static bool HasBranchAccess(this ICurrentUser user, Guid branchId) =>
        user.IsSystem || user.IsPlatformAdmin || user.HasTenantWideAccess || user.BranchIds.Contains(branchId);

    public static void EnsureBranchAccess(this ICurrentUser user, Guid branchId)
    {
        if (!user.HasBranchAccess(branchId))
            throw new ForbiddenAccessException("You do not have access to this branch.");
    }

    public static Guid RequireTenantId(this ICurrentUser user) =>
        user.TenantId ?? throw new ForbiddenAccessException("A tenant context is required for this operation.");
}
