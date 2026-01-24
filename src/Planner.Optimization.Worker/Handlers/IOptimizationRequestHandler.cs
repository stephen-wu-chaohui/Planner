using Planner.Messaging.Optimization;

namespace Planner.Optimization.Worker.Handlers;

public interface IOptimizationRequestHandler {
    Task HandleAsync(OptimizeRouteRequest request, CancellationToken ct);
}

