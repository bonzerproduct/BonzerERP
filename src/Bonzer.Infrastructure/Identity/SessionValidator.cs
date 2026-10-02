using Bonzer.Application.Common.Caching;

namespace Bonzer.Infrastructure.Identity;

/// <summary>
/// Called for every authenticated request (JwtBearer OnTokenValidated). Rejects access tokens of users that
/// were deactivated, or whose security stamp changed (password reset, role change, "log out everywhere"),
/// so these take effect immediately instead of when the 15-minute access token expires.
/// The check is cached for two minutes and the cache entry is removed whenever the user changes.
/// </summary>
public sealed class SessionValidator(BonzerDbContext db, ICacheService cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public sealed record SessionState(bool IsActive, string StampHash);

    public async Task<bool> IsValidAsync(Guid userId, string? stampHash, CancellationToken cancellationToken)
    {
        var state = await cache.GetOrCreateAsync(CacheKeys.UserSession(userId), async ct =>
            {
                var user = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.IsActive, u.SecurityStamp })
                    .FirstOrDefaultAsync(ct);
                return user is null ? null : new SessionState(user.IsActive, TokenService.StampHash(user.SecurityStamp));
            },
            CacheTtl, cancellationToken);

        return state is { IsActive: true } && state.StampHash == stampHash;
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(CacheKeys.UserSession(userId), cancellationToken);
}
