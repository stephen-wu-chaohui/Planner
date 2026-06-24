using Planner.Contracts.OptimizationRuns;

namespace Planner.Reactor;

public interface IOptimizationRunNotifier {
    Task SendRunChangedAsync(OptimizationRunChangedDto notification, CancellationToken ct);
    Task SendInsightChangedAsync(OptimizationInsightChangedDto notification, CancellationToken ct);
}
