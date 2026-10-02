using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bonzer.Infrastructure.Identity;

public sealed class UserAccountService(
    BonzerDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleResolver roles,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<UserAccountService> logger) : IUserAccountService
{
    public async Task<CreatedUserAccount> CreateAsync(CreateUserAccount request, CancellationToken cancellationToken = default)
    {
        EnsureSameTenant(request.TenantId);
        var resolved = await roles.ResolveAsync(request.TenantId, request.Roles, cancellationToken);
        await roles.EnsureCanGrantAsync(
            await roles.GetPermissionsOfRolesAsync(request.TenantId, request.Roles, cancellationToken),
            request.Roles, cancellationToken);

        var temporary = string.IsNullOrEmpty(request.Password);
        var password = temporary ? GenerateTemporaryPassword() : request.Password!;

        var user = new ApplicationUser
        {
            TenantId = request.TenantId,
            UserName = request.UserName,
            Email = request.Email,
            PhoneNumber = request.Phone,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserType = request.UserType,
            MustChangePassword = true,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
        ThrowIfFailed(await userManager.CreateAsync(user, password), "UserName");

        // Role rows are written by id: UserManager.AddToRoleAsync looks roles up by name only and could
        // pick another tenant's role with the same name.
        db.UserRoles.AddRange(resolved.Select(r => new IdentityUserRole<Guid> { UserId = user.Id, RoleId = r.Id }));
        db.UserBranchAccess.AddRange(request.BranchIds.Distinct().Select(b => new UserBranchAccess { UserId = user.Id, BranchId = b }));
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created {UserType} login {UserName} with roles {Roles}", request.UserType, user.UserName, request.Roles);
        return new CreatedUserAccount(user.Id, user.UserName!, temporary ? password : null);
    }

    public async Task<UserAccountInfo?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !CanAccess(user)) return null;

        var roleNames = await db.UserRoles.Where(ur => ur.UserId == userId)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);

        var lockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > clock.GetUtcNow();
        return new UserAccountInfo(user.Id, user.UserName!, user.IsActive, lockedOut, user.MustChangePassword, user.LastLoginAt, roleNames);
    }

    public async Task SetRolesAsync(Guid userId, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default)
    {
        if (userId == currentUser.UserId)
            throw new BusinessRuleException("You cannot change your own roles.");

        var user = await LoadAsync(userId, cancellationToken);
        var resolved = await roles.ResolveAsync(user.TenantId, roleNames, cancellationToken);

        var current = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        var currentNames = await db.Roles.Where(r => current.Select(c => c.RoleId).Contains(r.Id)).Select(r => r.Name!).ToListAsync(cancellationToken);

        // The caller must be allowed to grant the new roles and to take away the old ones.
        var touched = roleNames.Union(currentNames, StringComparer.OrdinalIgnoreCase).ToList();
        await roles.EnsureCanGrantAsync(
            await roles.GetPermissionsOfRolesAsync(user.TenantId, touched, cancellationToken), touched, cancellationToken);

        db.UserRoles.RemoveRange(current);
        db.UserRoles.AddRange(resolved.Select(r => new IdentityUserRole<Guid> { UserId = userId, RoleId = r.Id }));
        user.SecurityStamp = Guid.NewGuid().ToString();
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Roles of user {UserId} set to {Roles} by {ActorId}", userId, roleNames, currentUser.UserId);
    }

    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(userId, cancellationToken);
        if (user.IsActive == isActive) return;

        user.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        if (!isActive) await RevokeSessionsAsync(userId, "Account deactivated", cancellationToken);
    }

    public async Task<string> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(userId, cancellationToken);
        var password = GenerateTemporaryPassword();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        ThrowIfFailed(await userManager.ResetPasswordAsync(user, token, password), "Password");
        user.MustChangePassword = true;
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.UpdateAsync(user);
        await RevokeSessionsAsync(userId, "Password reset", cancellationToken);

        logger.LogInformation("Password of user {UserId} reset by {ActorId}", userId, currentUser.UserId);
        return password;
    }

    /// <summary>12 characters with upper, lower and digit; satisfies the Identity password policy.</summary>
    internal static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz", digits = "23456789";
        var chars = RandomNumberGenerator.GetItems<char>(upper, 3)
            .Concat(RandomNumberGenerator.GetItems<char>(lower, 6))
            .Concat(RandomNumberGenerator.GetItems<char>(digits, 3))
            .ToArray();
        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }

    private async Task<ApplicationUser> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is not null && CanAccess(user) ? user : throw new NotFoundException("User", userId);
    }

    private bool CanAccess(ApplicationUser user) =>
        currentUser.IsSystem || currentUser.IsPlatformAdmin || (user.TenantId is not null && user.TenantId == currentUser.TenantId);

    private void EnsureSameTenant(Guid tenantId)
    {
        if (!currentUser.IsSystem && !currentUser.IsPlatformAdmin && currentUser.TenantId != tenantId)
            throw new ForbiddenAccessException("Cannot create users for another institution.");
    }

    private Task RevokeSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.ReasonRevoked, reason), cancellationToken);
    }

    private static void ThrowIfFailed(IdentityResult result, string property)
    {
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure(property, e.Description)));
    }
}
