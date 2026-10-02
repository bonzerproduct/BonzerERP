namespace Bonzer.Infrastructure.Identity;

/// <summary>Branches a (non tenant-wide) user may work in. Emitted as "branch_id" claims in the JWT.</summary>
public class UserBranchAccess
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
