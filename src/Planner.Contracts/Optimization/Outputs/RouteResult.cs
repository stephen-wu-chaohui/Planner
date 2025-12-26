namespace Planner.Contracts.Optimization.Outputs;

public sealed class RouteResult {
    public int VehicleId { get; init; }

    public string VehicleName { get; init; } = string.Empty;

    public bool Used { get; init; }

    /// <summary>
    /// Ordered list of stops for this vehicle.
    /// If empty, the vehicle was not assigned any jobs.
    /// </summary>
    public IReadOnlyList<TaskAssignment> Stops { get; init; }
        = Array.Empty<TaskAssignment>();

    public double TotalMinutes { get; init; }

    public double TotalDistanceKm { get; init; }

    public double TotalCost { get; init; }
}
