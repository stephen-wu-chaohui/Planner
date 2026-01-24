using Planner.Contracts.Optimization.Responses;
using System;
using System.Linq;

namespace Planner.Optimization.SnapshotTests;

public static class ResponseNormalizer {
    public static object Normalize(OptimizeRouteResponse resp) =>
        new {
            resp.TenantId,
            resp.OptimizationRunId,
            resp.ErrorMessage,
            Routes = resp.Routes.Select(r => new {
                r.VehicleId,
                r.Used,
                Stops = r.Stops.Select(s => new {
                    s.JobId,
                    s.ArrivalTime,
                    s.DepartureTime
                }),
                TotalMinutes = Math.Round(r.TotalMinutes, 2),
                TotalDistanceKm = Math.Round(r.TotalDistanceKm, 3),
                TotalCost = Math.Round(r.TotalCost, 2)
            }),
            resp.ErrorMessage
        };
}
