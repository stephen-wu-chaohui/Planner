
namespace Planner.Contracts.Messages.VehicleRoutingProblem;

public class VrpRequestMessage
{
    public required string RequestId { get; init; }
    public required VrpRequest Request { get; init; }
}

public class VrpRequest
{
    public List<JobDto> Jobs { get; set; } = new();
    public List<VehicleDto> Vehicles { get; set; } = new();
    public DepotDto Depot { get; set; } = new();
    public double[][] DistanceMatrix { get; set; } = [];
}


public class JobDto
{
    public string Id { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Demand { get; set; } = 0;  // optional
}

public class VehicleDto
{
    public string Id { get; set; } = "";
    public double Capacity { get; set; } = 0;
}

public class DepotDto
{
    public string Id { get; set; } = "Depot";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
