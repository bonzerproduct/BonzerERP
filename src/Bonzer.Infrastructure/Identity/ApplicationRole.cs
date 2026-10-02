using Microsoft.AspNetCore.Identity;

namespace Bonzer.Infrastructure.Identity;

/// <summary>
/// Role with its permissions stored as role claims (type "permission").
/// TenantId = null for built-in system roles; set for roles a school creates itself.
/// Role names are unique per tenant (see TenantRoleValidator).
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public Guid? TenantId { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
