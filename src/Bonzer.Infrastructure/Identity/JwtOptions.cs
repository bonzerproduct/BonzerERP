using System.ComponentModel.DataAnnotations;

namespace Bonzer.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 key, at least 32 characters. Supply via user-secrets / environment (Jwt__SigningKey), never commit it.</summary>
    [Required, MinLength(32)] public string SigningKey { get; init; } = string.Empty;

    [Range(1, 1440)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(1, 365)] public int RefreshTokenDays { get; init; } = 7;
}
