using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.Application.OptimizationRuns;

public interface IOptimizationRunStore {
    Task<OptimizationRunDocument> CreateAsync(OptimizationRunDocument run, CancellationToken ct);
    Task<OptimizationRunDocument?> GetAsync(Guid tenantId, Guid runId, CancellationToken ct);
    Task MarkQueuedAsync(Guid tenantId, Guid runId, CancellationToken ct);
    Task<bool> TryStartAttemptAsync(Guid tenantId, Guid runId, OptimizationRunAttemptDto attempt, CancellationToken ct);
    Task SaveSolverResultAsync(Guid tenantId, Guid runId, OptimizeRouteResponse result, CancellationToken ct);
    Task SaveFailureAsync(Guid tenantId, Guid runId, string errorMessage, OptimizationRunStatus status, CancellationToken ct);
    Task SaveAiInsightAsync(Guid tenantId, Guid runId, OptimizationAiInsightDto insight, CancellationToken ct);
}
