namespace Planner.Domain;

public class Depot {
    public long Id { get; set; }            // persistence ID
    public Guid TenantId { get; init; }    // boundary ID
    public string Name { get; set; } = string.Empty;

    // location
    public long LocationId { get; set; }
    public required Location Location { get; set; }
}
