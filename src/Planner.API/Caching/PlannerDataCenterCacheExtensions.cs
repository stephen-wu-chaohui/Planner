using Planner.Infrastructure;

namespace Planner.API.Caching;

public static class PlannerDataCenterCacheExtensions {
    public static Task RemoveCacheKeysAsync(
        this IPlannerDataCenter dataCenter,
        CancellationToken cancellationToken = default,
        params string[] cacheKeys) =>
        dataCenter.RemoveCacheKeysAsync((IEnumerable<string>)cacheKeys, cancellationToken);

    public static async Task RemoveCacheKeysAsync(
        this IPlannerDataCenter dataCenter,
        IEnumerable<string> cacheKeys,
        CancellationToken cancellationToken = default) {
        foreach (var cacheKey in cacheKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.Ordinal)) {
            await dataCenter.Cache.RemoveAsync(cacheKey, cancellationToken);
        }
    }
}
