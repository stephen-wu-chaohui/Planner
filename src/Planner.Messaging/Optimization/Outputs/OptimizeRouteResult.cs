namespace Planner.Messaging.Optimization;

public sealed record OptimizeRouteResult(
    Guid TenantId,
    Guid OptimizationRunId,
    IReadOnlyList<RouteResult> Routes
);
