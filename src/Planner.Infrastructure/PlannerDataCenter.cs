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
            try {
                // Added explicit options for better compatibility
                return JsonSerializer.Deserialize<T>(bytes, new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });
            } catch (JsonException) {
                // If deserialization fails, treat it as a cache miss
                // rather than crashing the entire request.
                await cache.RemoveAsync(cacheKey, cancellationToken);
            }
        }

        // Step 2 – Cache miss (or corruption): fetch from The Vault
        var result = await fetchFromDb();

        // Step 3 – Populate The Workbench
        if (result is not null) {
            var options = new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
            };
            // Serialize with the same options used for deserialization
            var serializedData = JsonSerializer.SerializeToUtf8Bytes(result);
            await cache.SetAsync(cacheKey, serializedData, options, cancellationToken);
        }

        return result;
    }
}
