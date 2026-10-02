using System.Text;
using System.Text.Encodings.Web;
using Bonzer.Application.Features.Auth;
using Bonzer.Infrastructure.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bonzer.Infrastructure.Identity;

public sealed class AuthService(
    BonzerDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokens,
    IPermissionService permissions,
    IUserAccountService accounts,
    SecurityAuditor audit,
    IMessageSender messages,
    IOptions<MessagingOptions> messagingOptions,
    IHttpContextAccessor http,
    TimeProvider clock,
    IServiceProvider services,
    ILogger<AuthService> logger) : IAuthService
{
    private const string ResetPurpose = "ResetPassword";

    // ---------------- Login / tokens ----------------

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        var login = request.Login.Trim();

        var candidates = await FindByLoginAsync(login, cancellationToken);
        if (candidates.Count != 1)
        {
            await audit.RecordNowAsync(SecurityAuditor.LoginFailed, null, new { reason = candidates.Count == 0 ? "UnknownUser" : "Ambiguous" }, login, cancellationToken);
            throw candidates.Count > 1
                ? new UnauthorizedException("More than one account uses this email or phone. Sign in with your username.", ErrorCodes.AmbiguousLogin)
                : new UnauthorizedException("Invalid credentials.", ErrorCodes.InvalidCredentials);
        }

        var user = candidates[0];
        if (!user.IsActive)
        {
            await audit.RecordNowAsync(SecurityAuditor.LoginFailed, user, new { reason = "Disabled" }, cancellationToken: cancellationToken);
            throw new UnauthorizedException("This account is disabled. Contact your administrator.", ErrorCodes.AccountDisabled);
        }

        if (user.TenantId is { } tenantId && !await db.Tenants.AnyAsync(t => t.Id == tenantId && t.IsActive, cancellationToken))
            throw new UnauthorizedException("Your institution's subscription is inactive.", ErrorCodes.SubscriptionInactive);

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await audit.RecordNowAsync(SecurityAuditor.LoginFailed, user,
                new { reason = result.IsLockedOut ? "LockedOut" : "WrongPassword" }, cancellationToken: cancellationToken);
            throw result.IsLockedOut
                ? new UnauthorizedException("Account locked after repeated failed attempts. Try again in 15 minutes or ask your administrator to unlock it.",
                    ErrorCodes.AccountLocked)
                : new UnauthorizedException("Invalid credentials.", ErrorCodes.InvalidCredentials);
        }

        user.LastLoginAt = Now;
        audit.Record(SecurityAuditor.Login, user, new { userAgent = UserAgent });
        return await IssueTokensAsync(user, replacing: null, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hash = tokens.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User)
                         .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
                     ?? throw new UnauthorizedException("Invalid refresh token.", ErrorCodes.SessionExpired);

        if (stored.RevokedAt is not null)
        {
            // A rotated token was presented again: assume it was stolen and end every session of this user.
            if (stored.ReasonRevoked == "Rotated")
            {
                logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
                audit.Record(SecurityAuditor.TokenReuseDetected, stored.User);
                await accounts.EndAllSessionsAsync(stored.UserId, "Reuse detected", cancellationToken);
            }
            throw new UnauthorizedException("Session expired. Please sign in again.", ErrorCodes.SessionExpired);
        }

        if (!stored.IsActive(Now) || !stored.User.IsActive)
            throw new UnauthorizedException("Session expired. Please sign in again.", ErrorCodes.SessionExpired);

        return await IssueTokensAsync(stored.User, replacing: stored, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hash = tokens.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null || stored.RevokedAt is not null) return;

        Revoke(stored, "Logout");
        audit.Record(SecurityAuditor.Logout, stored.User);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId);
        audit.Record(SecurityAuditor.LogoutAll, user);
        await db.SaveChangesAsync(cancellationToken);
        await accounts.EndAllSessionsAsync(userId, "Logout everywhere", cancellationToken);
    }

    // ---------------- Profile ----------------

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId);
        return await ToProfileAsync(await BuildSubjectAsync(user, cancellationToken), cancellationToken);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        var user = await LoadUserAsync(userId);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName?.Trim();
        user.ProfilePhotoUrl = request.ProfilePhotoUrl;

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            user.NormalizedEmail = email is null ? null : userManager.NormalizeEmail(email);
            user.EmailConfirmed = false;
        }

        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (user.PhoneNumber != phone)
        {
            user.PhoneNumber = phone;
            user.PhoneNumberConfirmed = false;
        }

        ThrowIfFailed(await userManager.UpdateAsync(user), nameof(request.Email));
        return await ToProfileAsync(await BuildSubjectAsync(user, cancellationToken), cancellationToken);
    }

    // ---------------- Passwords ----------------

    public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        var user = await LoadUserAsync(userId);

        ThrowIfFailed(await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword), nameof(request.NewPassword));
        user.MustChangePassword = false;
        ThrowIfFailed(await userManager.UpdateAsync(user), nameof(request.NewPassword));

        // Every other device must sign in again; this one gets fresh tokens.
        await accounts.EndAllSessionsAsync(userId, "Password changed", cancellationToken);
        audit.Record(SecurityAuditor.PasswordChanged, user);
        return await IssueTokensAsync(user, replacing: null, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        var login = request.Login.Trim();
        var candidates = await FindByLoginAsync(login, cancellationToken);

        // Same response whether or not the account exists, so the endpoint cannot be used to discover users.
        if (candidates is not [{ IsActive: true } user])
        {
            await audit.RecordNowAsync(SecurityAuditor.PasswordResetRequested, null, new { found = false }, login, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await userManager.GeneratePasswordResetTokenAsync(user)));
            var link = QueryHelpers.AddQueryString(messagingOptions.Value.PasswordResetUrl,
                new Dictionary<string, string?> { ["login"] = user.UserName, ["token"] = token });
            var delivered = await TrySendAsync(user, "Email", () => messages.SendEmailAsync(user.Email, "Reset your Bonzer ERP password",
                $"<p>Hello {HtmlEncoder.Default.Encode(user.FirstName)},</p>" +
                $"<p>Click <a href=\"{HtmlEncoder.Default.Encode(link)}\">this link</a> to choose a new password. It expires in 30 minutes.</p>" +
                "<p>If you did not ask for this, you can ignore this email.</p>", cancellationToken));
            await audit.RecordNowAsync(SecurityAuditor.PasswordResetRequested, user, new { channel = "Email", delivered }, cancellationToken: cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            var code = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, ResetPurpose);
            var delivered = await TrySendAsync(user, "Sms", () => messages.SendSmsAsync(user.PhoneNumber,
                $"{code} is your Bonzer ERP password reset code. It is valid for 3 minutes. Do not share it.", cancellationToken));
            await audit.RecordNowAsync(SecurityAuditor.PasswordResetRequested, user, new { channel = "Sms", delivered }, cancellationToken: cancellationToken);
        }
        else
        {
            logger.LogWarning("Password reset requested for {UserId}, who has no email or phone; an administrator must reset it", user.Id);
            await audit.RecordNowAsync(SecurityAuditor.PasswordResetRequested, user, new { channel = "None" }, cancellationToken: cancellationToken);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        const string invalid = "The reset link or code is invalid or has expired. Request a new one.";

        var candidates = await FindByLoginAsync(request.Login.Trim(), cancellationToken);
        if (candidates is not [{ IsActive: true } user])
            throw new BusinessRuleException(invalid, ErrorCodes.InvalidResetToken);
        if (await userManager.IsLockedOutAsync(user))
            throw new BusinessRuleException("Too many wrong codes. Try again in 15 minutes.", ErrorCodes.AccountLocked);

        string resetToken;
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            if (!await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, ResetPurpose, request.Code))
            {
                await userManager.AccessFailedAsync(user); // wrong codes count towards lockout (brute-force protection)
                throw new BusinessRuleException(invalid, ErrorCodes.InvalidResetToken);
            }
            resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        }
        else
        {
            try
            {
                resetToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token!));
            }
            catch (FormatException)
            {
                throw new BusinessRuleException(invalid, ErrorCodes.InvalidResetToken);
            }
        }

        var result = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                throw new BusinessRuleException(invalid, ErrorCodes.InvalidResetToken);
            ThrowIfFailed(result, nameof(request.NewPassword));
        }

        user.MustChangePassword = false;
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.UpdateAsync(user);
        await accounts.EndAllSessionsAsync(user.Id, "Password reset", cancellationToken);
        await audit.RecordNowAsync(SecurityAuditor.PasswordReset, user, new { channel = request.Code is null ? "Email" : "Sms" }, cancellationToken: cancellationToken);
    }

    // ---------------- Sessions ----------------

    public async Task<IReadOnlyList<SessionDto>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = Now;
        return await db.RefreshTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SessionDto(t.Id, t.CreatedAt, t.ExpiresAt, t.CreatedByIp, t.UserAgent))
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId, cancellationToken)
                    ?? throw new NotFoundException("Session", sessionId);
        if (token.RevokedAt is not null) return;
        Revoke(token, "Revoked by user");
        await db.SaveChangesAsync(cancellationToken);
    }

    // ---------------- Helpers ----------------

    /// <summary>
    /// A provider failure must not change the forgot-password response: a 503 only for existing accounts
    /// would reveal which logins exist. The failure is logged (with the provider error) and audited instead.
    /// </summary>
    private async Task<bool> TrySendAsync(ApplicationUser user, string channel, Func<Task> send)
    {
        try
        {
            await send();
            return true;
        }
        catch (MessageDeliveryException ex)
        {
            logger.LogError(ex, "Password reset {Channel} for user {UserId} could not be delivered", channel, user.Id);
            return false;
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, RefreshToken? replacing, CancellationToken cancellationToken)
    {
        var subject = await BuildSubjectAsync(user, cancellationToken);
        var (accessToken, accessExpires) = tokens.CreateAccessToken(subject);
        var (refreshToken, refreshHash, refreshExpires) = tokens.CreateRefreshToken();

        if (replacing is not null)
        {
            Revoke(replacing, "Rotated");
            replacing.ReplacedByTokenHash = refreshHash;
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = UuidV7.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = Now,
            ExpiresAt = refreshExpires,
            CreatedByIp = IpAddress,
            UserAgent = UserAgent
        });
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, accessExpires, refreshToken, refreshExpires, await ToProfileAsync(subject, cancellationToken));
    }

    /// <summary>Username, email or phone. Returns up to two matches so ambiguous logins can be rejected.</summary>
    private async Task<List<ApplicationUser>> FindByLoginAsync(string login, CancellationToken cancellationToken)
    {
        var normalizedName = userManager.NormalizeName(login);
        var normalizedEmail = userManager.NormalizeEmail(login);
        return await userManager.Users
            .Where(u => u.NormalizedUserName == normalizedName || u.NormalizedEmail == normalizedEmail || u.PhoneNumber == login)
            .OrderBy(u => u.Id)
            .Take(2)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Collects roles, branches and the linked staff / guardian / student profile. Runs before the caller has
    /// a tenant claim, so tenant filters are bypassed explicitly and replaced by the user's own TenantId.
    /// </summary>
    private async Task<TokenSubject> BuildSubjectAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).Order().ToList();
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
        return new UserProfileDto(user.Id, user.UserName ?? string.Empty, user.Email, user.PhoneNumber, user.FullName,
            user.ProfilePhotoUrl, user.UserType, user.TenantId, subject.BranchIds, subject.Roles, granted.Order().ToList(),
            user.MustChangePassword, user.LastLoginAt);
    }

    private async Task<ApplicationUser> LoadUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("User", userId);

    private void Revoke(RefreshToken token, string reason)
    {
        token.RevokedAt = Now;
        token.RevokedByIp = IpAddress;
        token.ReasonRevoked = reason;
    }

    private async Task ValidateAsync<T>(T request, CancellationToken cancellationToken) =>
        await services.GetRequiredService<IValidator<T>>().ValidateAndThrowAsync(request, cancellationToken);

    private static void ThrowIfFailed(IdentityResult result, string property)
    {
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure(property, e.Description)));
    }

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private string? IpAddress => http.HttpContext?.Connection.RemoteIpAddress?.ToString();
    private string? UserAgent => http.HttpContext?.Request.Headers.UserAgent.ToString();
}
