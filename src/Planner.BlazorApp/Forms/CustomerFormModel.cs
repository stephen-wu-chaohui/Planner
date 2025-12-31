namespace Planner.BlazorApp.Forms;

/// <summary>
/// UI form model for editing customers.
/// Owns location data only at creation / edit time.
/// </summary>
public sealed record CustomerFormModel {
    /// <summary>
    /// Tenant-scoped customer identifier.
    /// </summary>
    public long CustomerId { get; init; }

    public string Name { get; set; } = string.Empty;

    // Location (immutable once persisted, but editable in UI form)
    public long LocationId { get; init; }

    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public long DefaultServiceMinutes { get; set; }

    public bool RequiresRefrigeration { get; set; }
    /// <summary>
    /// Default job type created from this customer.
    /// 0 = Depot, 1 = Delivery, 2 = Pickup
    /// </summary>
    public int DefaultJobType { get; set; }
}
