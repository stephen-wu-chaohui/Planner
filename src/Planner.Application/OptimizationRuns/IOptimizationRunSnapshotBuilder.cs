using Planner.Contracts.OptimizationRuns;

namespace Planner.Application.OptimizationRuns;

public interface IOptimizationRunSnapshotBuilder {
    Task<OptimizationRunDocument> BuildAsync(
        Guid tenantId,
        string? requestedBy,
        int? searchTimeLimitSeconds,
        CancellationToken ct);
}
