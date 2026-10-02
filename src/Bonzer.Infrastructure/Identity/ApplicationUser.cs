using Microsoft.AspNetCore.Identity;

namespace Bonzer.Infrastructure.Identity;

/// <summary>
/// The single login table for everyone (staff, parents, students, platform admins).
/// Replaces the duplicate custom "users" / "parent_users" tables that sat beside Identity.
/// The person record links back through Staff.UserId / Guardian.UserId / Student.UserId.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Null only for platform administrators.</summary>
    public Guid? TenantId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public UserType UserType { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserBranchAccess> BranchAccess { get; set; } = new List<UserBranchAccess>();

    public string FullName => string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}";
}
