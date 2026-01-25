using Planner.Messaging.Optimization.Inputs;
using System;
using System.Linq;

namespace Planner.API.EndToEndTests.Snapshot;

public static class OptimizeRouteRequestSnapshot {
    public static object Create(OptimizeRouteRequest req) {
        return new {
            req.TenantId,
            req.OptimizationRunId,

            Stops = req.Stops
                .OrderBy(j => j.LocationId)
                .Select(j => new {
                    j.LocationId,
                    j.LocationType,
                    j.ServiceTimeMinutes,
                    j.ReadyTime,
                    j.DueTime,
                    j.PalletDemand,
                    j.WeightDemand,
                    j.RequiresRefrigeration,
                }),

            Vehicles = req.Vehicles
                .OrderBy(v => v.VehicleId)
                .Select(v => new {
                    v.VehicleId,
                    v.ShiftLimitMinutes,
                    StartLocationId = v.StartDepotLocationId,
                    EndLocationId = v.EndDepotLocationId,
                    v.SpeedFactor,
                    CostPerMinute = Math.Round(v.CostPerMinute, 4),
                    CostPerKm = Math.Round(v.CostPerKm, 4),
                    v.BaseFee,
                    v.MaxPallets,
                    v.MaxWeight,
                    v.RefrigeratedCapacity
                })
        };
    }
}
