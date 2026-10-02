namespace Bonzer.Application.Features.Auth;

/// <summary>Login with username, email or phone. Usernames are globally unique (e.g. "dps.priya" or an employee code).</summary>
public sealed record LoginRequest(string Login, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Starts password recovery: a reset link is emailed, or a 6-digit code is sent by SMS when there is no email.</summary>
public sealed record ForgotPasswordRequest(string Login);

/// <summary>Completes recovery with either the emailed <paramref name="Token"/> or the SMS <paramref name="Code"/>.</summary>
public sealed record ResetPasswordRequest(string Login, string? Token, string? Code, string NewPassword);

public sealed record UpdateProfileRequest(string FirstName, string? LastName, string? Email, string? PhoneNumber, string? ProfilePhotoUrl);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string UserName,
    string? Email,
    string? PhoneNumber,
    string FullName,
    string? ProfilePhotoUrl,
    UserType UserType,
    Guid? TenantId,
    IReadOnlyList<Guid> BranchIds,
    IReadOnlyList<string> Roles,
    IReadOnlyCollection<string> Permissions,
    bool MustChangePassword,
    DateTime? LastLoginAt);

/// <summary>A signed-in device / browser (one refresh-token chain).</summary>
public sealed record SessionDto(Guid Id, DateTime CreatedAt, DateTime ExpiresAt, string? IpAddress, string? UserAgent);

public sealed record MessageResponse(string Message);

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Rotates the refresh token. Re-use of a revoked token revokes every session of the user.</summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ends every session on every device, including access tokens already issued.</summary>
    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Changes the password, ends all other sessions and returns fresh tokens for this one.</summary>
    Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Always succeeds (does not reveal whether the account exists).</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionDto>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
}

internal static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an upper-case letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lower-case letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password.");
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() => RuleFor(x => x.Login).NotEmpty().MaximumLength(256);
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(256);
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Token) ^ !string.IsNullOrWhiteSpace(x.Code))
            .WithName("Token").WithMessage("Provide either the reset token from the email or the code from the SMS.");
        RuleFor(x => x.Code).Matches(@"^\d{6}$").When(x => !string.IsNullOrWhiteSpace(x.Code)).WithMessage("The code has 6 digits.");
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneNumber).Matches(@"^\+?[0-9]{10,15}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Enter a valid phone number.");
        RuleFor(x => x.ProfilePhotoUrl).MaximumLength(1000)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfilePhotoUrl)).WithMessage("Photo must be an absolute http(s) URL.");
    }
}
