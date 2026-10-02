using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Bonzer.Infrastructure.Identity;

/// <summary>
/// Writes security events (logins, failed logins, logouts, password resets, role changes) to audit_logs,
/// alongside the automatic entity audit trail, so administrators can see who signed in from where.
/// </summary>
public sealed class SecurityAuditor(BonzerDbContext db, ICurrentUser currentUser, TimeProvider clock, ILogger<SecurityAuditor> logger)
{
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string LogoutAll = "LogoutAll";
    public const string PasswordChanged = "PasswordChanged";
    public const string PasswordResetRequested = "PasswordResetRequested";
    public const string PasswordReset = "PasswordReset";
    public const string RolesChanged = "RolesChanged";
    public const string AccountStatusChanged = "AccountStatusChanged";
    public const string AccountUnlocked = "AccountUnlocked";
    public const string TokenReuseDetected = "TokenReuseDetected";

    /// <summary>Queues the event; it is saved with the caller's next SaveChanges (same transaction).</summary>
    public void Record(string action, ApplicationUser? user, object? details = null, string? login = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = user?.TenantId ?? currentUser.TenantId,
            UserId = currentUser.UserId ?? user?.Id,
            UserName = currentUser.UserName ?? user?.UserName ?? login,
            Action = action,
            EntityName = "User",
            EntityId = user?.Id.ToString(),
            NewValues = details is null ? null : JsonSerializer.Serialize(details),
            IpAddress = currentUser.IpAddress,
            CorrelationId = currentUser.CorrelationId,
            Timestamp = clock.GetUtcNow().UtcDateTime
        });
        logger.LogInformation("Security event {Action} for {UserName}", action, user?.UserName ?? login);
    }

    /// <summary>Records and saves immediately (for failures, where nothing else is saved).</summary>
    public async Task RecordNowAsync(string action, ApplicationUser? user, object? details = null, string? login = null,
        CancellationToken cancellationToken = default)
    {
        Record(action, user, details, login);
        await db.SaveChangesAsync(cancellationToken);
    }
}
