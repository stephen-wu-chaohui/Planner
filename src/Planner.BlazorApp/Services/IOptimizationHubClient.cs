namespace Planner.BlazorApp.Services;

using Planner.Contracts.Messaging.Events;

public interface IOptimizationHubClient : IAsyncDisposable {
    Task ConnectAsync(Guid tenantId, Guid? optimizationRunId = null);
    Task DisconnectAsync();

    event Action<RouteOptimizedEvent> OptimizationCompleted;
}
