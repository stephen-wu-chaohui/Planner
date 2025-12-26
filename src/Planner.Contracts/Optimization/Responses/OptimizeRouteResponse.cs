using Planner.Contracts.Optimization.Outputs;

namespace Planner.Contracts.Optimization.Responses;

public sealed class OptimizeRouteResponse {
    public Guid TenantId { get; init; }
    public Guid OptimizationRunId { get; init; }

    public DateTime CompletedAt { get; init; }

    public IReadOnlyList<RouteResult> Routes { get; init; } = Array.Empty<RouteResult>();

    public double TotalCost { get; init; }
}
