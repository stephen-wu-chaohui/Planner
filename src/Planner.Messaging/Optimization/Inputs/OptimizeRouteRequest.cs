using System.Collections.ObjectModel;

namespace Planner.Messaging.Optimization.Inputs;

/// <summary>
/// Represents a request to optimize vehicle routes.
/// </summary>
/// <param name="TenantId">Multi-tenant security boundary used for orchestration.</param>
/// <param name="OptimizationRunId">Correlation ID for a single optimization execution.</param>
/// <param name="RequestedAt">Timestamp when the command was created (UTC).</param>
/// <param name="Vehicles">Vehicles participating in this optimization run.</param>
/// <param name="Stops">Stops to be optimized, containing location and constraints.</param>
/// <param name="DistanceMatrix">Precomputed distance matrix (in kilometers) between all locations. Required.</param>
/// <param name="TravelTimeMatrix">Precomputed travel time matrix (in minutes) between all locations. Required.</param>
/// <param name="Settings">Specific optimization solver settings and magic numbers.</param>
public sealed record OptimizeRouteRequest(
    Guid TenantId,
    Guid OptimizationRunId,
    DateTime RequestedAt,
    VehicleInput[] Vehicles,
    StopInput[] Stops,
    long[] DistanceMatrix,    // Length = (Stops.Length x Stops.Length)
    long[] TravelTimeMatrix,  // Length = (Stops.Length x Stops.Length)
    OptimizationSettings? Settings = null
);
