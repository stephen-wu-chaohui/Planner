using Planner.Contracts.Optimization.Inputs;

namespace Planner.Contracts.Optimization.Requests;

public sealed class OptimizeRouteRequest {
    /// <summary>
    /// Multi-tenant security boundary.
    /// Used only for orchestration, not solver logic.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Correlation ID for a single optimization execution.
    /// Used for retries, idempotency, and result matching.
    /// </summary>
    public Guid OptimizationRunId { get; init; }

    /// <summary>
    /// Timestamp when the command was created (UTC).
    /// </summary>
    public DateTime RequestedAt { get; init; }

    /// <summary>
    /// Vehicles participating in this optimization run.
    /// </summary>
    public IReadOnlyList<VehicleInput> Vehicles { get; init; } = Array.Empty<VehicleInput>();

    /// <summary>
    /// Jobs to be optimized.
    /// Jobs are solver-facing and contain their own location and constraints.
    /// </summary>
    public IReadOnlyList<JobInput> Jobs { get; init; } = Array.Empty<JobInput>();

    /// <summary>
    /// Depots.
    /// </summary>
    public IReadOnlyList<DepotInput> Depots { get; init; } = Array.Empty<DepotInput>();

    /// <summary>
    /// Multiplier applied to cost calculation for overtime minutes.
    /// </summary>
    public double OvertimeMultiplier { get; init; } = 2.0;
}
