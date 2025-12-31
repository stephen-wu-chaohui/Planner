namespace Planner.Domain;

public class RouteStop {
    public long Id { get; set; }
    public Guid TenantId { get; init; }    // boundary ID

    public long RouteId { get; set; }
    public long TaskId { get; init; }   // NOT JobId
    public int Sequence { get; init; }
    public long ArrivalTimeMinutes { get; init; }
}
