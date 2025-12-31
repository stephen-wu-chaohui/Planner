namespace Planner.Domain;

public sealed class Customer {
    public long CustomerId { get; set; }
    public Guid TenantId { get; init; }    // boundary ID
    public string Name { get; set; } = string.Empty;

    public Location Location { get; set; } = default!;

    // Defaults for job creation
    public long DefaultServiceMinutes { get; set; }
    public bool RequiresRefrigeration { get; set; }
}
