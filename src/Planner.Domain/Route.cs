namespace Planner.Domain;

public class Route {
    public long Id { get; set; }
    public Guid TenantId { get; init; }    // boundary ID
    public Guid OptimizationRunId { get; init; }   // NOT JobId

    public int VehicleId { get; set; }

    public int TotalDistanceKm { get; set; }
    public int TotalCost { get; set; }
    public List<RouteStop> Stops { get; set; } = [];
}
