using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure;

/// <summary>
/// Implements <see cref="IPlannerDataCenter"/> with the Cache-Aside Pattern.
/// Coordinates between The Vault (SQL via <see cref="IPlannerDbContext"/>) and
/// The Workbench (Redis via <see cref="IDistributedCache"/>) to serve data efficiently.
/// </summary>
public sealed class PlannerDataCenter(IPlannerDbContext dbContext, IDistributedCache cache) : IPlannerDataCenter {
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public IPlannerDbContext DbContext => dbContext;

    /// <inheritdoc />
    public IDistributedCache Cache => cache;

    /// <inheritdoc />
    public async Task<T?> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T?>> fetchFromDb,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) {

        // Step 1 – Try The Workbench (Redis short-term memory)
        var bytes = await cache.GetAsync(cacheKey, cancellationToken);
        if (bytes is not null) {
            return JsonSerializer.Deserialize<T>(bytes);
        }

        // Step 2 – Cache miss: fetch from The Vault (SQL long-term memory)
        var result = await fetchFromDb();

        // Step 3 – Populate The Workbench for subsequent requests
        if (result is not null) {
            var options = new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
            };
            await cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(result), options, cancellationToken);
        }

        return result;
    }
}
