using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;

namespace Planner.BlazorApp.Services;

/// <summary>
/// Service that listens for optimization run notifications.
/// </summary>
public interface IOptimizationResultsListenerService : IAsyncDisposable
{
    event Action<OptimizationRunChangedDto>? OnOptimizationRunChanged;
    event Action<RoutingResultDto>? OnOptimizationCompleted;
    Task StartListeningAsync(Guid tenantId);
    Task StopListeningAsync();
}
