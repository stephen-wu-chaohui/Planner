using Planner.Application.Messaging;
using Planner.Contracts.Optimization;

namespace Planner.API.Services;

/// <summary>
/// Publishes routing results to connected SignalR clients.
/// </summary>
public interface IRouteService {
    Task PublishAsync(RoutingResultDto result);
}

/// <summary>
/// Scoped service for broadcasting routing results.
/// </summary>
public sealed class RouteService(IMessageHubPublisher hub) : IRouteService {
    public Task PublishAsync(RoutingResultDto result) {
        ArgumentNullException.ThrowIfNull(result);
        return hub.PublishAsync(nameof(RoutingResultDto), result);
    }
}
