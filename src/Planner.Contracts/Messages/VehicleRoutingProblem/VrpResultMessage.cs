// ----------- Route Stop Record -----------

using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Domain.Entities;

/// <summary>
/// Describes one stop in a vehicle route.
/// </summary>
public record RouteStop(
    int JobId,
    string JobName,
    JobType JobType,
    double ArrivalTime,
    double DepartureTime,
    long PalletLoad,
    long WeightLoad,
    long RefrigeratedLoad
);

// ----------- VRP Result Models -----------

public class VehicleRoute {
    public int VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public bool Used { get; set; }

    /// <summary>
    /// Ordered list of stops for this vehicle.
    /// If empty, it means this vehicle was not assigned any jobs.
    /// </summary>
    public List<RouteStop> Stops { get; set; } = new();

    public double TotalMinutes { get; set; }
    public double DistanceKm { get; set; }
    public double Cost { get; set; }
}

public class VrpResult {
    public List<VehicleRoute> Routes { get; set; } = new();
    public double TotalCost { get; set; }
}

// ----------- VRP Result Message -----------

public class VrpResultMessage {
    public Guid RequestId { get; set; }          // link back to VrpRequestMessage
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public VrpResult Result { get; set; } = new();
}

