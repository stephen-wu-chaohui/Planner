using System;
using Planner.Contracts.API;

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
/// Maps UI job form models to contracts.
/// Maps contracts to UI job form models.
/// </summary>
public static class JobMapper {
    public static JobDto ToDto(this JobFormModel form, long orderId = 0, long customerId = 0, string reference = "") {
        ArgumentNullException.ThrowIfNull(form);
        return new JobDto(
            Id: form.JobId,
            Name: form.Name,
            OrderId: orderId,
            CustomerId: customerId,
            JobType: form.JobType switch {
                0 => JobTypeDto.Depot,
                1 => JobTypeDto.Delivery,
                2 => JobTypeDto.Pickup,
                _ => throw new ArgumentOutOfRangeException(nameof(form.JobType), form.JobType, "Unknown job type")
            },
            Reference: reference,
            Location: new LocationDto(form.LocationId, string.Empty, form.Latitude, form.Longitude),
            ServiceTimeMinutes: form.ServiceTimeMinutes,
            PalletDemand: form.PalletDemand,
            WeightDemand: form.WeightDemand,
            ReadyTime: form.ReadyTime,
            DueTime: form.DueTime,
            RequiresRefrigeration: form.RequiresRefrigeration
        );
    }

    public static JobFormModel ToFormModel(this JobDto dto) {
        ArgumentNullException.ThrowIfNull(dto);
        return new JobFormModel {
            JobId = dto.Id,
            JobType = dto.JobType switch {
                JobTypeDto.Depot => 0,
                JobTypeDto.Delivery => 1,
                JobTypeDto.Pickup => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(dto.JobType), dto.JobType, "Unknown job type")
            },
            Name = dto.Name,
            LocationId = dto.Location.Id,
            Latitude = dto.Location.Latitude,
            Longitude = dto.Location.Longitude,
            ServiceTimeMinutes = dto.ServiceTimeMinutes,
            ReadyTime = dto.ReadyTime,
            DueTime = dto.DueTime,
            PalletDemand = dto.PalletDemand,
            WeightDemand = dto.WeightDemand,
            RequiresRefrigeration = dto.RequiresRefrigeration
        };
    }

}
