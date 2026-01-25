using Planner.Contracts.Optimization;

namespace Planner.BlazorApp.Services;


public interface IOptimizationHubClient : IAsyncDisposable {
    Task ConnectAsync(Guid? optimizationRunId = null);
    Task DisconnectAsync();

    event Action<RoutingResultDto> OptimizationCompleted;
}
