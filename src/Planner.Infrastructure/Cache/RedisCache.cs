using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Planner.Infrastructure.Cache;

/// <summary>
/// Redis-backed implementation of <see cref="ICache"/> using <see cref="IDistributedCache"/>.
/// Acts as "The Workbench" – fast short-term memory that sits in front of the SQL database.
/// </summary>
public sealed class RedisCache(IDistributedCache distributedCache) : ICache {
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) {
        var bytes = await distributedCache.GetAsync(key, cancellationToken);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        var options = new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await distributedCache.SetAsync(key, bytes, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        await distributedCache.RemoveAsync(key, cancellationToken);
    }
}
