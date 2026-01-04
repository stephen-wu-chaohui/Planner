using Planner.Contracts.Optimization.Requests;
using System;
using System.Linq;

namespace Planner.API.EndToEndTests.Snapshot;

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
                    j.ServiceTimeMinutes,
                    j.ReadyTime,
                    j.DueTime,
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
                    v.ShiftLimitMinutes,
                    StartLocationId = v.StartLocation.LocationId,
                    EndLocationId = v.EndLocation.LocationId,
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
