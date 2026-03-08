using Microsoft.Extensions.Caching.Distributed;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure;

/// <summary>
/// Central data-access facade that unifies the two memory tiers of the Planner system:
/// <list type="bullet">
///   <item><description>
///     <b>The Vault</b> – SQL Server via <see cref="IPlannerDbContext"/> (long-term memory,
///     authoritative source of truth).
///   </description></item>
///   <item><description>
///     <b>The Workbench</b> – Redis via <see cref="IDistributedCache"/> (short-term memory,
///     fast in-flight cache for frequently-read data).
///   </description></item>
/// </list>
/// Both REST controllers and GraphQL resolvers inject this interface as their single point of
/// data access, enabling the Cache-Aside Pattern transparently via
/// <see cref="GetOrFetchAsync{T}"/>.
/// </summary>
public interface IPlannerDataCenter {
    /// <summary>
    /// The Vault – SQL Server long-term memory for persistent, authoritative data access.
    /// </summary>
    IPlannerDbContext DbContext { get; }

    /// <summary>
    /// The Workbench – Redis short-term memory for fast, low-latency cached data access.
    /// </summary>
    IDistributedCache Cache { get; }

    /// <summary>
    /// Applies the <b>Cache-Aside Pattern</b>:
    /// <list type="number">
    ///   <item><description>Read from The Workbench (Redis cache) first.</description></item>
    ///   <item><description>On a cache miss, fetch from The Vault (SQL database).</description></item>
    ///   <item><description>Populate The Workbench so subsequent reads are served from cache.</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="cacheKey">Unique cache key.</param>
    /// <param name="fetchFromDb">Delegate that queries The Vault on a cache miss.</param>
    /// <param name="expiry">Optional cache entry lifetime; defaults to 5 minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or freshly-fetched value, or <c>null</c> if not found.</returns>
    Task<T?> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T?>> fetchFromDb,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);
}
