namespace Planner.Contracts.Optimization;

/// <summary>
/// Ordered list of stops for this vehicle.
/// If empty, the vehicle was not assigned any jobs.
/// </summary>
public sealed record RouteDto(
    long VehicleId,
    IReadOnlyList<TaskAssignmentDto> Stops,
    double TotalMinutes,
    double TotalDistanceKm,
    double TotalCost,
    string? VehicleName = null
);
