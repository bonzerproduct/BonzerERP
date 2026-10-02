using Microsoft.AspNetCore.Identity;

namespace Bonzer.Infrastructure.Identity;

/// <summary>
/// Replaces Identity's default RoleValidator, which forces role names to be globally unique.
/// Here names are unique per tenant, so every school can have its own "Exam Coordinator" role.
/// </summary>
public sealed class TenantRoleValidator(BonzerDbContext db) : IRoleValidator<ApplicationRole>
{
    public async Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role)
    {
        var name = await manager.GetRoleNameAsync(role);
        if (string.IsNullOrWhiteSpace(name))
            return IdentityResult.Failed(manager.ErrorDescriber.InvalidRoleName(name));

        var normalized = manager.NormalizeKey(name);
        var duplicate = await db.Roles.AnyAsync(r =>
            r.Id != role.Id && r.NormalizedName == normalized && r.TenantId == role.TenantId);

        return duplicate
            ? IdentityResult.Failed(manager.ErrorDescriber.DuplicateRoleName(name))
            : IdentityResult.Success;
    }
}
