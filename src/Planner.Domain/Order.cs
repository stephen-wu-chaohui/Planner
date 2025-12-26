namespace Planner.Domain.Entities;

public class Order {
    public int Id { get; set; }              // persistence ID
    public Guid TenantId { get; init; }      // boundary ID

    public Guid OrderPublicId { get; init; } // boundary / external-safe

    public string CustomerName { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
}
