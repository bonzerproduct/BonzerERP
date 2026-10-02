using Bonzer.Application.Features.Auth;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bonzer.Infrastructure.Identity;

public sealed class AuthService(
    BonzerDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokens,
    IPermissionService permissions,
    IHttpContextAccessor http,
    TimeProvider clock,
    IValidator<LoginRequest> loginValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var login = request.Login.Trim();
        var normalizedName = userManager.NormalizeName(login);
        var normalizedEmail = userManager.NormalizeEmail(login);

        // Username, email or phone. Ambiguous matches (shared email / phone) must use the username.
        var candidates = await userManager.Users
            .Where(u => u.NormalizedUserName == normalizedName || u.NormalizedEmail == normalizedEmail || u.PhoneNumber == login)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (candidates.Count != 1)
        {
            logger.LogWarning("Login failed for {Login}: {Count} matching accounts", login, candidates.Count);
            throw new UnauthorizedException(candidates.Count > 1
                ? "More than one account uses this email or phone. Sign in with your username."
                : "Invalid credentials.");
        }

        var user = candidates[0];
        if (!user.IsActive)
            throw new UnauthorizedException("This account is disabled.");

        if (user.TenantId is { } tenantId &&
            !await db.Tenants.AnyAsync(t => t.Id == tenantId && t.IsActive, cancellationToken))
            throw new UnauthorizedException("Your institution's subscription is inactive.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            throw new UnauthorizedException("Account locked after repeated failed attempts. Try again later.");
        if (!result.Succeeded)
            throw new UnauthorizedException();

        user.LastLoginAt = Now;
        logger.LogInformation("User {UserId} signed in", user.Id);
        return await IssueTokensAsync(user, replacing: null, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hash = tokens.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User)
                         .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
                     ?? throw new UnauthorizedException("Invalid refresh token.");

        if (stored.RevokedAt is not null)
        {
            // A rotated token was presented again: assume it was stolen and end every session of this user.
            logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
            await RevokeAllAsync(stored.UserId, "Reuse detected", cancellationToken);
            throw new UnauthorizedException("Session expired. Please sign in again.");
        }

        if (!stored.IsActive(Now) || !stored.User.IsActive)
            throw new UnauthorizedException("Session expired. Please sign in again.");

        return await IssueTokensAsync(stored.User, replacing: stored, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hash = tokens.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null || stored.RevokedAt is not null) return;

        stored.RevokedAt = Now;
        stored.RevokedByIp = IpAddress;
        stored.ReasonRevoked = "Logout";
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("User", userId);
        var subject = await BuildSubjectAsync(user, cancellationToken);
        return await ToProfileAsync(subject, cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        await changePasswordValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("User", userId);

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure(nameof(request.NewPassword), e.Description)));

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        await RevokeAllAsync(user.Id, "Password changed", cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, RefreshToken? replacing, CancellationToken cancellationToken)
    {
        var subject = await BuildSubjectAsync(user, cancellationToken);
        var (accessToken, accessExpires) = tokens.CreateAccessToken(subject);
        var (refreshToken, refreshHash, refreshExpires) = tokens.CreateRefreshToken();

        if (replacing is not null)
        {
            replacing.RevokedAt = Now;
            replacing.RevokedByIp = IpAddress;
            replacing.ReasonRevoked = "Rotated";
            replacing.ReplacedByTokenHash = refreshHash;
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = Now,
            ExpiresAt = refreshExpires,
            CreatedByIp = IpAddress,
            UserAgent = http.HttpContext?.Request.Headers.UserAgent.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, accessExpires, refreshToken, refreshExpires,
            await ToProfileAsync(subject, cancellationToken));
    }

    /// <summary>
    /// Collects roles, branches and the linked staff / guardian / student profile. Runs before the caller has
    /// a tenant claim, so tenant filters are bypassed explicitly and replaced by the user's own TenantId.
    /// </summary>
    private async Task<TokenSubject> BuildSubjectAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var tenantId = user.TenantId;

        var branchIds = await db.UserBranchAccess.Where(b => b.UserId == user.Id).Select(b => b.BranchId).ToListAsync(cancellationToken);

        Guid? staffId = null, guardianId = null, studentId = null;
        switch (user.UserType)
        {
            case UserType.Staff:
                var staff = await db.Staff.IgnoreQueryFilters()
                    .Where(s => s.UserId == user.Id && s.TenantId == tenantId && !s.IsDeleted)
                    .Select(s => new { s.Id, s.BranchId }).FirstOrDefaultAsync(cancellationToken);
                staffId = staff?.Id;
                if (staff is not null && !branchIds.Contains(staff.BranchId)) branchIds.Add(staff.BranchId);
                break;
            case UserType.Guardian:
                guardianId = await db.Guardians.IgnoreQueryFilters()
                    .Where(g => g.UserId == user.Id && g.TenantId == tenantId && !g.IsDeleted)
                    .Select(g => (Guid?)g.Id).FirstOrDefaultAsync(cancellationToken);
                break;
            case UserType.Student:
                var student = await db.Students.IgnoreQueryFilters()
                    .Where(s => s.UserId == user.Id && s.TenantId == tenantId && !s.IsDeleted)
                    .Select(s => new { s.Id, s.BranchId }).FirstOrDefaultAsync(cancellationToken);
                studentId = student?.Id;
                if (student is not null && !branchIds.Contains(student.BranchId)) branchIds.Add(student.BranchId);
                break;
        }

        return new TokenSubject(user, roles, branchIds, staffId, guardianId, studentId);
    }

    private async Task<UserProfileDto> ToProfileAsync(TokenSubject subject, CancellationToken cancellationToken)
    {
        var user = subject.User;
        var granted = await permissions.GetPermissionsAsync(user.TenantId, subject.Roles, cancellationToken);
        return new UserProfileDto(user.Id, user.UserName ?? string.Empty, user.Email, user.FullName, user.UserType,
            user.TenantId, subject.BranchIds, subject.Roles, granted.Order().ToList(), user.MustChangePassword);
    }

    private Task RevokeAllAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = Now;
        var ip = IpAddress;
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedByIp, ip)
                .SetProperty(t => t.ReasonRevoked, reason), cancellationToken);
    }

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private string? IpAddress => http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
