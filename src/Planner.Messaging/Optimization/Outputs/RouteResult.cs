namespace Planner.Messaging.Optimization.Outputs;

/// <summary>
/// Ordered list of stops for this vehicle.
/// If empty, the vehicle was not assigned any jobs.
/// </summary>
public sealed record RouteResult(
    long VehicleId,
    IReadOnlyList<TaskAssignment> Stops,
    double TotalMinutes,
    double TotalDistanceKm,
    double TotalCost
);
