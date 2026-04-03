using Microsoft.Extensions.Caching.Hybrid;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure;

/// <summary>
/// Implements <see cref="IPlannerDataCenter"/> with the Cache-Aside Pattern.
/// Coordinates between The Vault (SQL via <see cref="IPlannerDbContext"/>) and
/// The Workbench (HybridCache) to serve data efficiently.
/// </summary>
public sealed class PlannerDataCenter(IPlannerDbContext dbContext, HybridCache cache) : IPlannerDataCenter {
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public IPlannerDbContext DbContext => dbContext;

    /// <inheritdoc />
    public HybridCache Cache => cache;

    /// <inheritdoc />
    public async Task<T?> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T?>> fetchFromDb,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) {

        var options = new HybridCacheEntryOptions {
            Expiration = expiry ?? DefaultExpiry,
            LocalCacheExpiration = expiry ?? DefaultExpiry,
        };

        // HybridCache.GetOrCreateAsync handles L1 (in-process) lookup, factory invocation on miss,
        // and L2 (optional distributed) population automatically.
        return await cache.GetOrCreateAsync<T?>(
            cacheKey,
            async ct => await fetchFromDb(),
            options,
            cancellationToken: cancellationToken);
    }
}
