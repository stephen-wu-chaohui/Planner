using Planner.Infrastructure.Cache;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure;

/// <summary>
/// Implements <see cref="IPlannerDataCenter"/> with the Cache-Aside Pattern.
/// Coordinates between The Vault (SQL via <see cref="IPlannerDbContext"/>) and
/// The Workbench (Redis via <see cref="ICache"/>) to serve data efficiently.
/// </summary>
public sealed class PlannerDataCenter(IPlannerDbContext dbContext, ICache cache) : IPlannerDataCenter {
    /// <inheritdoc />
    public IPlannerDbContext DbContext => dbContext;

    /// <inheritdoc />
    public ICache Cache => cache;

    /// <inheritdoc />
    public async Task<T?> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T?>> fetchFromDb,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) {

        // Step 1 – Try The Workbench (Redis short-term memory)
        var cached = await cache.GetAsync<T>(cacheKey, cancellationToken);
        if (cached is not null) {
            return cached;
        }

        // Step 2 – Cache miss: fetch from The Vault (SQL long-term memory)
        var result = await fetchFromDb();

        // Step 3 – Populate The Workbench for subsequent requests
        if (result is not null) {
            await cache.SetAsync(cacheKey, result, expiry, cancellationToken);
        }

        return result;
    }
}
