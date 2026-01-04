using Planner.Contracts.Optimization.Requests;
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
                    j.Name,
                    j.ReadyTime,
                    j.DueTime,
                    j.ServiceTimeMinutes,
                    j.PalletDemand,
                    j.WeightDemand,
                    j.RequiresRefrigeration,
                    Location = new {
                        j.Location.LocationId,
                        j.Location.Latitude,
                        j.Location.Longitude
                    }
                }),

            Vehicles = req.Vehicles
                .OrderBy(v => v.VehicleId)
                .Select(v => new {
                    v.VehicleId,
                    v.Name,
                    StartLocationId = v.StartLocation.LocationId,
                    EndLocationId = v.EndLocation.LocationId,
                    v.ShiftLimitMinutes,
                    v.SpeedFactor,
                    v.CostPerMinute,
                    v.CostPerKm,
                    v.BaseFee,
                    v.MaxPallets,
                    v.MaxWeight,
                    v.RefrigeratedCapacity
                })
        };
    }
}
