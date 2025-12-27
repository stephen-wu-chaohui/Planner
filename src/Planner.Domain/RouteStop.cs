public class RouteStop {
    public int Id { get; set; }
    public Guid TenantId { get; init; }    // boundary ID

    public int RouteId { get; set; }
    public int TaskId { get; init; }   // NOT JobId

    public int Sequence { get; init; }
    public long ArrivalTimeMinutes { get; init; }
}
