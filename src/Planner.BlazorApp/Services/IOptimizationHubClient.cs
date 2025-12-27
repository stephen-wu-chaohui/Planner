namespace Planner.BlazorApp.Services;

using Planner.Contracts.Optimization.Responses;

public interface IOptimizationHubClient : IAsyncDisposable {
    Task ConnectAsync(Guid tenantId, Guid? optimizationRunId = null);
    Task DisconnectAsync();

    event Action<OptimizeRouteResponse> OptimizationCompleted;
}
