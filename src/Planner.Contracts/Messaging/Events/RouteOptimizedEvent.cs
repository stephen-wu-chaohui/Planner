namespace Planner.Contracts.Messaging.Events;

using Planner.Contracts.Optimization.Responses;

/// <summary>
/// Event published when a route optimization run has completed successfully.
/// </summary>
public sealed class RouteOptimizedEvent {
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
    /// Optimization result payload.
    /// </summary>
    public OptimizeRouteResponse Result { get; init; } = default!;
}
