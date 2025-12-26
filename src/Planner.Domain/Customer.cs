namespace Planner.Domain;

public sealed class Customer {
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Location Location { get; set; } = default!;

    // Defaults for job creation
    public long DefaultServiceMinutes { get; set; }
    public bool RequiresRefrigeration { get; set; }
}
