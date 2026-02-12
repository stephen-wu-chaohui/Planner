using DomainLocation = Planner.Domain.Location;
using DomainVehicle = Planner.Domain.Vehicle;
using DomainJob = Planner.Domain.Job;
using DomainJobType = Planner.Domain.JobType;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.Services;

public static class ToInput
{
    public static StopInput FromJob(DomainJob job) {
        return new StopInput(
            LocationId: ToLocationId(job.Location),
            LocationType: ToContractJobType(job.JobType),
            ServiceTimeMinutes: job.ServiceTimeMinutes,
            ReadyTime: job.ReadyTime,
            DueTime: job.DueTime,
            PalletDemand: job.PalletDemand,
            WeightDemand: job.WeightDemand,
            RequiresRefrigeration: job.RequiresRefrigeration,
            ExtraIdForJob: job.Id
        );
    }

    public static StopInput FromDepotLocation(long DepotLocationId) {
        return new StopInput(
            LocationId: DepotLocationId,
            LocationType: 0, // Depot
            ServiceTimeMinutes: 0,
            ReadyTime: 0,
            DueTime: 0,
            PalletDemand: 0,
            WeightDemand: 0,
            RequiresRefrigeration: false,
            ExtraIdForJob: null
        );
    }

    public static VehicleInput FromVehicle(DomainVehicle vehicle) {
        var costPerMinute = (vehicle.DriverRatePerHour + vehicle.MaintenanceRatePerHour) / 60.0;

        if (vehicle.StartDepot?.Location is null)
            throw new InvalidOperationException($"Vehicle {vehicle.Id} missing StartDepot/Location");
        if (vehicle.EndDepot?.Location is null)
            throw new InvalidOperationException($"Vehicle {vehicle.Id} missing EndDepot/Location");

        return new VehicleInput(
            VehicleId: vehicle.Id,
            ShiftLimitMinutes: vehicle.ShiftLimitMinutes,
            StartDepotLocationId: vehicle.StartDepot.Location.Id,
            EndDepotLocationId: vehicle.EndDepot.Location.Id,
            SpeedFactor: vehicle.SpeedFactor,
            CostPerMinute: costPerMinute,
            CostPerKm: vehicle.FuelRatePerKm,
            BaseFee: vehicle.BaseFee,
            MaxPallets: vehicle.MaxPallets,
            MaxWeight: vehicle.MaxWeight,
            RefrigeratedCapacity: vehicle.RefrigeratedCapacity
        );
    }

    public static long ToLocationId(DomainLocation location) {
        return location.Id;
    }

    public static int ToContractJobType(DomainJobType jobType) {
        // Domain enum is: 0 Depot, 1 Pickup, 2 Delivery
        // Contract expects: 0 Depot, 1 Delivery, 2 Pickup
        return jobType switch {
            DomainJobType.Depot => 0,
            DomainJobType.Delivery => 1,
            DomainJobType.Pickup => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown job type.")
        };
    }

}