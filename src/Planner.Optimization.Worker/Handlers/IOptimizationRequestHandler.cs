using Planner.Contracts.Optimization.Requests;

namespace Planner.Optimization.Worker.Handlers;
public interface IOptimizationRequestHandler {
    Task HandleAsync(OptimizeRouteRequest request, CancellationToken ct);
}

