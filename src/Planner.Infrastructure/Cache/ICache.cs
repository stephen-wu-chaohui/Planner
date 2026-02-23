namespace Planner.Infrastructure.Cache;

/// <summary>
/// Abstraction for short-term memory cache (The Workbench).
/// Implementations may use Redis or any distributed/in-memory cache provider.
/// </summary>
public interface ICache {
    /// <summary>Retrieve a cached value by key, or <c>null</c> if not present.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Store a value in the cache with an optional expiry window.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>Evict a cached entry by key.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
