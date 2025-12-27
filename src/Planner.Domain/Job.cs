
using Planner.Domain;

public enum JobType { Depot, Pickup, Delivery }

public class Job {
    public int Id { get; set; }            // persistence ID
    public Guid TenantId { get; init; }    // boundary ID

    public required string Name { get; set; }

    public int OrderId { get; set; }        // FK to Order.Id
    public int CustomerID { get; set; }
    public JobType JobType { get; set; }

    // business identity
    public string Reference { get; set; } = string.Empty;

    // location
    public required Location Location { get; set; }

    // constraints
    public long ServiceTimeMinutes { get; set; }
    public long PalletDemand { get; set; }
    public long WeightDemand { get; set; }
    public long ReadyTime { get; set; }
    public long DueTime { get; set; }
    public bool RequiresRefrigeration { get; set; }
}
