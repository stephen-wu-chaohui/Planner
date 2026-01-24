using Planner.Messaging.Optimization.Requests;

namespace Planner.Messaging.Optimization;

/// <summary>
/// Represents a request to optimize vehicle routes.
/// </summary>
/// <param name="TenantId">Multi-tenant security boundary used for orchestration.</param>
/// <param name="OptimizationRunId">Correlation ID for a single optimization execution.</param>
/// <param name="RequestedAt">Timestamp when the command was created (UTC).</param>
/// <param name="Vehicles">Vehicles participating in this optimization run.</param>
/// <param name="Jobs">Jobs to be optimized, containing location and constraints.</param>
/// <param name="DistanceMatrix">Precomputed distance matrix (in kilometers) between all locations. Required.</param>
/// <param name="TravelTimeMatrix">Precomputed travel time matrix (in minutes) between all locations. Required.</param>
/// <param name="OvertimeMultiplier">Multiplier applied to cost calculation for overtime minutes.</param>
/// <param name="Settings">Specific optimization solver settings and magic numbers.</param>
public sealed record OptimizeRouteRequest(
    Guid TenantId,
    Guid OptimizationRunId,
    DateTime RequestedAt,
    IReadOnlyList<VehicleInput> Vehicles,
    IReadOnlyList<JobInput> Jobs,
    long[][] DistanceMatrix,
    long[][] TravelTimeMatrix,
    double OvertimeMultiplier = 2.0,
    OptimizationSettings? Settings = null
);