namespace Bonzer.Application.Common.Interfaces;

public sealed record CreateUserAccount(
    Guid TenantId,
    string UserName,
    string? Email,
    string? Phone,
    string FirstName,
    string? LastName,
    UserType UserType,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    string? Password); // null = generate a temporary password (user must change it at first login)

public sealed record CreatedUserAccount(Guid UserId, string UserName, string? TemporaryPassword);

public sealed record UserAccountInfo(
    Guid UserId, string UserName, bool IsActive, bool IsLockedOut, bool MustChangePassword,
    DateTime? LastLoginAt, IReadOnlyList<string> Roles);

/// <summary>
/// Login accounts and role assignment, independent of ASP.NET Identity. Every call enforces that the
/// caller can only grant roles whose permissions they hold themselves (no privilege escalation).
/// </summary>
public interface IUserAccountService
{
    Task<CreatedUserAccount> CreateAsync(CreateUserAccount request, CancellationToken cancellationToken = default);

    Task<UserAccountInfo?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SetRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    /// <summary>Deactivating also ends every session (refresh tokens are revoked).</summary>
    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Sets a new temporary password and returns it once.</summary>
    Task<string> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default);
}
