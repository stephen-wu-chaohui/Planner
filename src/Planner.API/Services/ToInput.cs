using Planner.Domain;
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.Services;

public static class ToInput
{

    public static JobInput ToJobInput(Job job) {
        return new JobInput(
            JobId: job.Id,
            JobType: ToContractJobType(job.JobType),
            Location: ToLocationInput(job.Location),
            ServiceTimeMinutes: job.ServiceTimeMinutes,
            ReadyTime: job.ReadyTime,
            DueTime: job.DueTime,
            PalletDemand: job.PalletDemand,
            WeightDemand: job.WeightDemand,
            RequiresRefrigeration: job.RequiresRefrigeration
        );
    }

    public static VehicleInput ToVehicleInput(Vehicle vehicle) {
        var costPerMinute = (vehicle.DriverRatePerHour + vehicle.MaintenanceRatePerHour) / 60.0;

        if (vehicle.StartDepot?.Location is null)
            throw new InvalidOperationException($"Vehicle {vehicle.Id} missing StartDepot/Location");
        if (vehicle.EndDepot?.Location is null)
            throw new InvalidOperationException($"Vehicle {vehicle.Id} missing EndDepot/Location");

        return new VehicleInput(
            VehicleId: vehicle.Id,
            ShiftLimitMinutes: vehicle.ShiftLimitMinutes,
            StartLocation: vehicle.DepotStartId,
            EndLocation: vehicle.DepotEndId,
            SpeedFactor: vehicle.SpeedFactor,
            CostPerMinute: costPerMinute,
            CostPerKm: vehicle.FuelRatePerKm,
            BaseFee: vehicle.BaseFee,
            MaxPallets: vehicle.MaxPallets,
            MaxWeight: vehicle.MaxWeight,
            RefrigeratedCapacity: vehicle.RefrigeratedCapacity
        );
    }

    public static long ToLocationInput(Location location) {
        return location.Id;
    }

    public static int ToContractJobType(JobType jobType) {
        // Domain enum is: 0 Depot, 1 Pickup, 2 Delivery
        // Contract expects: 0 Depot, 1 Delivery, 2 Pickup
        return jobType switch {
            JobType.Depot => 0,
            JobType.Delivery => 1,
            JobType.Pickup => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown job type.")
        };
    }

}