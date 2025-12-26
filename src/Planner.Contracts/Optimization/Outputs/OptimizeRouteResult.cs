namespace Planner.Contracts.Optimization.Outputs;

public sealed class OptimizeRouteResult {
    public Guid TenantId { get; init; }
    public Guid OptimizationRunId { get; init; }

    public IReadOnlyList<RouteResult> Routes { get; init; } = [];
}
