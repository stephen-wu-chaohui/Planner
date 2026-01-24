using Planner.Contracts.Optimization.Outputs;

namespace Planner.Contracts.Optimization.Responses;

/// <summary>
/// Represents the response from a route optimization request.
/// </summary>
/// <param name="TenantId">Multi-tenant security boundary.</param>
/// <param name="OptimizationRunId">Correlation ID for the optimization run.</param>
/// <param name="CompletedAt">UTC timestamp when optimization completed.</param>
/// <param name="Routes">Generated routes.</param>
/// <param name="TotalCost">Total cost associated with the optimization.</param>
/// <param name="ErrorMessage">Error message explaining any optimization failure or incomplete results. Null on success.</param>
public sealed record OptimizeRouteResponse(
    Guid TenantId,
    Guid OptimizationRunId,
    DateTime CompletedAt,
    IReadOnlyList<RouteResult> Routes,
    double TotalCost,
    string? ErrorMessage = null
);
