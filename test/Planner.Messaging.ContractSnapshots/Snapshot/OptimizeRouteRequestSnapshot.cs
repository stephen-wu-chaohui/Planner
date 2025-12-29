using Planner.Contracts.Optimization.Requests;
using System.Linq;

namespace Planner.Messaging.ContractSnapshots.Snapshot;

public static class OptimizeRouteRequestSnapshot {
    public static object Create(OptimizeRouteRequest req) {
        return new {
            req.TenantId,
            req.OptimizationRunId,

            Depots = req.Depots
                .OrderBy(d => d.Location.LocationId)
                .Select(d => new {
                    d.Location.LocationId,
                    d.Location.Address,
                    d.Location.Latitude,
                    d.Location.Longitude
                }),

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
                    v.DepotStartId,
                    v.DepotEndId,
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
