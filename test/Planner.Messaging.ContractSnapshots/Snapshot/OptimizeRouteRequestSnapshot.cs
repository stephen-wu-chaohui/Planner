global using Planner.Messaging.Optimization;
using System.Linq;

namespace Planner.Messaging.ContractSnapshots.Snapshot;

public static class OptimizeRouteRequestSnapshot {
    public static object Create(OptimizeRouteRequest req) {
        return new {
            req.TenantId,
            req.OptimizationRunId,

            Jobs = req.Jobs
                .OrderBy(j => j.JobId)
                .Select(j => new {
                    j.JobId,
                    j.JobType,
                    j.ReadyTime,
                    j.DueTime,
                    j.ServiceTimeMinutes,
                    j.PalletDemand,
                    j.WeightDemand,
                    j.RequiresRefrigeration,
                }),

            Vehicles = req.Vehicles
                .OrderBy(v => v.VehicleId)
                .Select(v => new {
                    v.VehicleId,
                    StartLocationId = v.StartLocation,
                    EndLocationId = v.EndLocation,
                    v.ShiftLimitMinutes,
                    v.SpeedFactor,
                    v.CostPerMinute,
                    v.CostPerKm,
                    v.BaseFee,
                    v.MaxPallets,
                    v.MaxWeight,
                    v.RefrigeratedCapacity
                }),
            
            // Note: DistanceMatrix and TravelTimeMatrix are large arrays, so we only show if they're present
            HasDistanceMatrix = req.DistanceMatrix != null,
            HasTravelTimeMatrix = req.TravelTimeMatrix != null
        };
    }
}
