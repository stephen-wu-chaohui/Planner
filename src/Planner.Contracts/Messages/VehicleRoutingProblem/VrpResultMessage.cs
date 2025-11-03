namespace Planner.Contracts.Messages.VehicleRoutingProblem;

public class VrpResultMessage {
    public required string RequestId { get; init; }
    public required DateTime CompletedAt { get; init; }
    public required VrpResult Response { get; init; }
}

public class VrpResult {
    public List<VehicleRoute> Vehicles { get; set; } = new();
    public double TotalDistance { get; set; }
    public double ObjectiveValue { get; set; }
    public string SolverStatus { get; set; } = "Unknown";
}

public class VehicleRoute {
    public string VehicleId { get; set; } = "";
    public List<RouteStop> Stops { get; set; } = new();
    public double RouteDistance { get; set; }
}

public class RouteStop {
    public string JobId { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceFromPrevious { get; set; }
}
