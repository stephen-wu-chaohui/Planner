using Planner.Domain;

namespace Planner.BlazorApp.Forms;

/// <summary>
/// UI form model for defining optimization jobs.
/// Location is reference-only and not editable.
/// </summary>
public sealed class JobFormModel {
    public long JobId { get; set; }

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


/// <summary>
/// Maps UI job form models to contracts.  Use Domain.Job as the contract here.
/// Maps contracts to UI job form models.  Use Domain.Job as the contract here.
/// </summary>
public static class JobMapper {
    public static Job ToContract(JobFormModel form) {
        ArgumentNullException.ThrowIfNull(form);
        return new Job { 
            Id = form.JobId,
            JobType = (JobType)form.JobType,
            Name = form.Name,
            Location = new Location(form.LocationId, "", form.Latitude, form.Longitude),
            ServiceTimeMinutes = form.ServiceTimeMinutes,
            ReadyTime = form.ReadyTime,
            DueTime = form.DueTime,
            PalletDemand = form.PalletDemand,
            WeightDemand = form.WeightDemand,
            RequiresRefrigeration = form.RequiresRefrigeration
        };
    }

    public static JobFormModel ToFormModel(Job job) {
        ArgumentNullException.ThrowIfNull(job);
        return new JobFormModel {
            JobId = job.Id,
            JobType = (int)job.JobType,
            Name = job.Name,
            LocationId = job.Location.Id,
            Latitude = job.Location.Latitude,
            Longitude = job.Location.Longitude,
            ServiceTimeMinutes = job.ServiceTimeMinutes,
            ReadyTime = job.ReadyTime,
            DueTime = job.DueTime,
            PalletDemand = job.PalletDemand,
            WeightDemand = job.WeightDemand,
            RequiresRefrigeration = job.RequiresRefrigeration
        };
    }
}
