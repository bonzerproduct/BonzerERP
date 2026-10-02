using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Bonzer.Application.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bonzer.Infrastructure.Identity;

public sealed record TokenSubject(
    ApplicationUser User,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    Guid? StaffId,
    Guid? GuardianId,
    Guid? StudentId);

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(TokenSubject subject);
    (string Token, string Hash, DateTime ExpiresAt) CreateRefreshToken();
    string Hash(string refreshToken);
}

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime ExpiresAt) CreateAccessToken(TokenSubject subject)
    {
        var user = subject.User;
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.GivenName, user.FullName),
            new(AppClaimTypes.UserType, user.UserType.ToString()),
            new(AppClaimTypes.SecurityStamp, StampHash(user.SecurityStamp))
        };
        if (user.MustChangePassword) claims.Add(new(AppClaimTypes.PasswordChangeRequired, "1"));
        if (!string.IsNullOrEmpty(user.Email)) claims.Add(new(JwtRegisteredClaimNames.Email, user.Email));
        if (user.TenantId.HasValue) claims.Add(new(AppClaimTypes.TenantId, user.TenantId.Value.ToString()));
        if (subject.StaffId.HasValue) claims.Add(new(AppClaimTypes.StaffId, subject.StaffId.Value.ToString()));
        if (subject.GuardianId.HasValue) claims.Add(new(AppClaimTypes.GuardianId, subject.GuardianId.Value.ToString()));
        if (subject.StudentId.HasValue) claims.Add(new(AppClaimTypes.StudentId, subject.StudentId.Value.ToString()));
        claims.AddRange(subject.BranchIds.Select(b => new Claim(AppClaimTypes.BranchId, b.ToString())));
        claims.AddRange(subject.Roles.Select(r => new Claim(AppClaimTypes.Role, r)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256)
        };

        return (_handler.CreateToken(descriptor), expires);
    }

    public (string Token, string Hash, DateTime ExpiresAt) CreateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (token, Hash(token), clock.GetUtcNow().UtcDateTime.AddDays(_options.RefreshTokenDays));
    }

    public string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    /// <summary>Short hash of the security stamp: lets us detect stale tokens without exposing the stamp.</summary>
    public static string StampHash(string? securityStamp) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp ?? string.Empty)))[..16];
}
