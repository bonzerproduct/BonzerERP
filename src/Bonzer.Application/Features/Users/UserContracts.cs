namespace Bonzer.Application.Features.Users;

public sealed class UserQuery : PagedRequest
{
    public UserType? UserType { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public Guid? BranchId { get; set; }
}

public sealed record UserListItemDto(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    string? PhoneNumber,
    UserType UserType,
    bool IsActive,
    bool IsLockedOut,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record BranchRefDto(Guid Id, string Name);

/// <summary>The person record this login belongs to (staff member, guardian or student).</summary>
public sealed record LinkedProfileDto(string Type, Guid Id, string Name);

public sealed record UserDetailDto(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    string? PhoneNumber,
    UserType UserType,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset? LockoutEnd,
    int FailedLoginCount,
    bool MustChangePassword,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<BranchRefDto> Branches,
    LinkedProfileDto? LinkedProfile,
    int ActiveSessions);

public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles);

public sealed record SetUserStatusRequest(bool IsActive);

public sealed record SetUserBranchesRequest(IReadOnlyList<Guid> BranchIds);

/// <param name="UserName">Empty = admission number (students) or phone number (parents).</param>
/// <param name="Password">Empty = a temporary password is generated and returned once.</param>
public sealed record CreatePortalLoginRequest(string? UserName, string? Password);

public sealed record TemporaryPasswordResponse(string TemporaryPassword);

/// <summary>
/// Central administration of every login in the institution: staff, parents and students.
/// Staff logins are also created from the Staff module (POST /staff/{id}/login).
/// </summary>
public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default);
    Task<UserDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetRolesAsync(Guid id, SetUserRolesRequest request, CancellationToken cancellationToken = default);
    Task SetStatusAsync(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken = default);
    Task SetBranchesAsync(Guid id, SetUserBranchesRequest request, CancellationToken cancellationToken = default);
    Task UnlockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TemporaryPasswordResponse> ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default);
    Task LogoutEverywhereAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Parent portal login for a guardian (role Parent). One login covers all of their children.</summary>
    Task<CreatedUserAccount> CreateGuardianLoginAsync(Guid guardianId, CreatePortalLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Student portal login (role Student), typically for higher classes, college and tuition.</summary>
    Task<CreatedUserAccount> CreateStudentLoginAsync(Guid studentId, CreatePortalLoginRequest request, CancellationToken cancellationToken = default);
}

public sealed class CreatePortalLoginRequestValidator : AbstractValidator<CreatePortalLoginRequest>
{
    public CreatePortalLoginRequestValidator()
    {
        RuleFor(x => x.UserName).Matches(@"^[a-zA-Z0-9._@+-]{3,64}$").When(x => !string.IsNullOrWhiteSpace(x.UserName))
            .WithMessage("Username: 3-64 characters, letters, digits and . _ @ + - only.");
        RuleFor(x => x.Password).MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public sealed class SetUserRolesRequestValidator : AbstractValidator<SetUserRolesRequest>
{
    public SetUserRolesRequestValidator() => RuleFor(x => x.Roles).NotEmpty().WithMessage("Assign at least one role.");
}
