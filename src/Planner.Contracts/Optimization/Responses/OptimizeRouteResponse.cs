using Planner.Contracts.Optimization.Outputs;

namespace Planner.Contracts.Optimization.Responses;

public sealed class OptimizeRouteResponse {
    /// <summary>
    /// Multi-tenant security boundary.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Correlation ID for the optimization run.
    /// </summary>
    public Guid OptimizationRunId { get; init; }

    /// <summary>
    /// UTC timestamp when optimization completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Generated routes
    /// </summary>
    public IReadOnlyList<RouteResult> Routes { get; init; } = Array.Empty<RouteResult>();

    /// <summary>
    /// Gets the total cost associated with the current instance.
    /// </summary>
    public double TotalCost { get; init; }
}
