using Planner.Domain.Entities;

namespace Planner.Contracts.Messages.VehicleRoutingProblem;


// ----------- VRP Request -----------

public class VrpRequest {
    public List<Job> Jobs { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();

    // Use jagged arrays for JSON friendliness
    public double[][] DistanceKm { get; set; } = default!;
    public double[][] TravelMinutes { get; set; } = default!;
}

// ----------- VRP Request Message -----------

public class VrpRequestMessage {
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTime? CompletedAt { get; set; }
    public VrpRequest Request { get; set; } = new();
}


