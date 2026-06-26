using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;

namespace Planner.Application.OptimizationRuns;

public interface IOptimizationRunNotificationPublisher {
    Task PublishRunChangedAsync(OptimizationRunChangedDto notification, CancellationToken ct);
    Task PublishInsightChangedAsync(OptimizationInsightChangedDto notification, CancellationToken ct);
    Task PublishOptimizationCompletedAsync(RoutingResultDto result, CancellationToken ct);
}

internal sealed class NoopOptimizationRunNotificationPublisher : IOptimizationRunNotificationPublisher {
    public Task PublishRunChangedAsync(OptimizationRunChangedDto notification, CancellationToken ct) =>
        Task.CompletedTask;

    public Task PublishInsightChangedAsync(OptimizationInsightChangedDto notification, CancellationToken ct) =>
        Task.CompletedTask;

    public Task PublishOptimizationCompletedAsync(RoutingResultDto result, CancellationToken ct) =>
        Task.CompletedTask;
}
