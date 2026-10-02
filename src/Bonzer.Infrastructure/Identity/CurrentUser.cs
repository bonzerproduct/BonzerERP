using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Http;

namespace Bonzer.Infrastructure.Identity;

/// <summary>Reads the caller from the validated JWT. Claim names are not remapped (MapInboundClaims = false).</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    // No HTTP request => migrations, seeding, background jobs. Trusted code only.
    public bool IsSystem => accessor.HttpContext is null;

    public Guid? UserId => GetGuid(JwtRegisteredClaimNames.Sub);
    public string? UserName => Principal?.FindFirstValue(JwtRegisteredClaimNames.Name);
    public Guid? TenantId => GetGuid(AppClaimTypes.TenantId);

    public UserType? UserType =>
        Enum.TryParse<UserType>(Principal?.FindFirstValue(AppClaimTypes.UserType), out var t) ? t : null;

    public IReadOnlyList<Guid> BranchIds =>
        Principal?.FindAll(AppClaimTypes.BranchId).Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty).ToList() ?? [];

    public IReadOnlyList<string> Roles => Principal?.FindAll(AppClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public Guid? StaffId => GetGuid(AppClaimTypes.StaffId);
    public Guid? GuardianId => GetGuid(AppClaimTypes.GuardianId);
    public Guid? StudentId => GetGuid(AppClaimTypes.StudentId);

    public bool IsPlatformAdmin => IsAuthenticated && UserType == Domain.Enums.UserType.PlatformAdmin && IsInRole(AppRoles.SuperAdmin);

    public bool HasTenantWideAccess => Roles.Any(AppRoles.TenantWide.Contains);

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    private Guid? GetGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
