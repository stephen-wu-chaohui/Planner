namespace Planner.BlazorApp.Forms;

/// <summary>
/// UI form model for defining optimization jobs.
/// Location is reference-only and not editable.
/// </summary>
public sealed class JobFormModel {
    public int JobId { get; set; }

    /// <summary>
    /// 0 = Depot, 1 = Delivery, 2 = Pickup
    /// </summary>
    public int JobType { get; set; }

    public string Name { get; set; } = string.Empty;

    // --- Location (reference only) ---
    public long LocationId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    // --- Time constraints ---
    public long ServiceTimeMinutes { get; set; }
    public long ReadyTime { get; set; }
    public long DueTime { get; set; }

    // --- Capacity / constraints ---
    public long PalletDemand { get; set; }
    public long WeightDemand { get; set; }
    public bool RequiresRefrigeration { get; set; }
}
