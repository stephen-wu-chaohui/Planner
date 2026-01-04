using Planner.Contracts.Optimization.Inputs;

namespace Planner.Contracts.Optimization.Requests;

/// <summary>
/// Represents a request to optimize vehicle routes.
/// </summary>
/// <param name="TenantId">Multi-tenant security boundary used for orchestration.</param>
/// <param name="OptimizationRunId">Correlation ID for a single optimization execution.</param>
/// <param name="RequestedAt">Timestamp when the command was created (UTC).</param>
/// <param name="Vehicles">Vehicles participating in this optimization run.</param>
/// <param name="Jobs">Jobs to be optimized, containing location and constraints.</param>
/// <param name="Depots">Depots available for the vehicles.</param>
/// <param name="OvertimeMultiplier">Multiplier applied to cost calculation for overtime minutes.</param>
/// <param name="Settings">Specific optimization solver settings and magic numbers.</param>
public sealed record OptimizeRouteRequest(
    Guid TenantId,
    Guid OptimizationRunId,
    DateTime RequestedAt,
    IReadOnlyList<VehicleInput> Vehicles,
    IReadOnlyList<JobInput> Jobs,
    double OvertimeMultiplier = 2.0,
    OptimizationSettings? Settings = null
);