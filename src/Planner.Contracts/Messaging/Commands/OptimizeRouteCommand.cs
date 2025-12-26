namespace Planner.Contracts.Messaging.Commands;

using Planner.Contracts.Optimization.Requests;

/// <summary>
/// Command sent to the optimization worker to start a route optimization run.
/// </summary>
public sealed class OptimizeRouteCommand {
    /// <summary>
    /// Multi-tenant security boundary.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Correlation ID for a single optimization execution.
    /// </summary>
    public Guid OptimizationRunId { get; init; }

    /// <summary>
    /// Timestamp when the command was created (UTC).
    /// </summary>
    public DateTime RequestedAt { get; init; }

    /// <summary>
    /// Optimization request payload.
    /// </summary>
    public OptimizeRouteRequest Request { get; init; } = default!;
}
