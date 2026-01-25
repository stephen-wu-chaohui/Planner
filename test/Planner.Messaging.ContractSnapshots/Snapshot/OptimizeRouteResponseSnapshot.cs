using Planner.Messaging.Optimization.Outputs;
using System;
using System.Linq;

namespace Planner.Messaging.ContractSnapshots.Snapshot;

public static class OptimizeRouteResponseSnapshot {
    public static object Create(OptimizeRouteResponse resp) {
        return new {
            resp.TenantId,
            resp.OptimizationRunId,

            Routes = resp.Routes
                .OrderBy(r => r.VehicleId)
                .Select(r => new {
                    r.VehicleId,
                    Stops = r.Stops
                        .OrderBy(s => s.ArrivalTime)
                        .ThenBy(s => s.LocationId)
                        .Select(s => new {
                            s.LocationId,
                            ArrivalTime = Math.Round(s.ArrivalTime, 2),
                            DepartureTime = Math.Round(s.DepartureTime, 2),
                            s.PalletLoad,
                            s.WeightLoad,
                            s.RefrigeratedLoad
                        }),

                    TotalMinutes = Math.Round(r.TotalMinutes, 2),
                    TotalDistanceKm = Math.Round(r.TotalDistanceKm, 3),
                    TotalCost = Math.Round(r.TotalCost, 2)
                }),

            TotalCost = Math.Round(resp.TotalCost, 2),
            resp.ErrorMessage
        };
    }
}
