namespace Planner.Contracts.Optimization.Outputs;

public sealed record OptimizeRouteResult(
    Guid TenantId,
    Guid OptimizationRunId,
    IReadOnlyList<RouteResult> Routes
);
