using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Bonzer.Infrastructure.Caching;

/// <summary>
/// JSON-serialising wrapper over IDistributedCache (Redis). A Redis outage degrades to "no cache":
/// after a failure the cache is bypassed for <see cref="BreakDuration"/> (a simple circuit breaker), so
/// requests hit the database directly instead of each waiting for the Redis connect timeout.
/// Entries removed during an outage can be served stale after Redis returns, until their TTL expires.
/// </summary>
public sealed class DistributedCacheService(
    IDistributedCache cache,
    TimeProvider clock,
    ILogger<DistributedCacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private long _openUntilTicks; // registered as a singleton, so this is shared by all requests

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var (found, value) = await TryGetAsync<T>(key, cancellationToken);
        return found ? value : default;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) =>
        ExecuteAsync("write", key, () => cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value, Json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl }, cancellationToken));

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var (found, cached) = await TryGetAsync<T>(key, cancellationToken);
        if (found) return cached!;

        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, cancellationToken);
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        ExecuteAsync("remove", key, () => cache.RemoveAsync(key, cancellationToken));

    private async Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken)
    {
        byte[]? bytes = null;
        await ExecuteAsync("read", key, async () => bytes = await cache.GetAsync(key, cancellationToken));
        return bytes is null ? (false, default) : (true, JsonSerializer.Deserialize<T>(bytes, Json));
    }

    private async Task ExecuteAsync(string operation, string key, Func<Task> action)
    {
        if (clock.GetUtcNow().UtcTicks < Interlocked.Read(ref _openUntilTicks)) return;

        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Interlocked.Exchange(ref _openUntilTicks, clock.GetUtcNow().Add(BreakDuration).UtcTicks);
            logger.LogWarning(ex, "Cache {Operation} failed for {CacheKey}; bypassing cache for {Seconds}s",
                operation, key, BreakDuration.TotalSeconds);
        }
    }
}
