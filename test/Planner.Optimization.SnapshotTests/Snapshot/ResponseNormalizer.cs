using Planner.Messaging.Optimization.Outputs;
using System;
using System.Linq;

namespace Planner.Optimization.SnapshotTests.Snapshot;

public static class ResponseNormalizer {
    public static object Normalize(OptimizeRouteResponse resp) =>
        new {
            resp.TenantId,
            resp.OptimizationRunId,
            resp.ErrorMessage,
            Routes = resp.Routes.Select(r => new {
                r.VehicleId,
                Stops = r.Stops.Select(s => new {
                    s.LocationId,
                    s.ArrivalTime,
                    s.DepartureTime
                }),
                TotalMinutes = Math.Round(r.TotalMinutes, 2),
                TotalDistanceKm = Math.Round(r.TotalDistanceKm, 3),
                TotalCost = Math.Round(r.TotalCost, 2)
            })
        };
}
