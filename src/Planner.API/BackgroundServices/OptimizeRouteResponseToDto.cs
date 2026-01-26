using Planner.Contracts.API;
using Planner.Contracts.Optimization;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.API.BackgroundServices;

static public class OptimizeRouteResponseToDto {
    public static RoutingResultDto ToDto(this OptimizeRouteResponse resp) {
        return new RoutingResultDto(
            resp.TenantId,
            resp.OptimizationRunId,
            resp.CompletedAt,
            resp.Routes.Select(r => r.ToDto()).ToList(),
            resp.TotalCost);
    }

    public static RouteDto ToDto(this RouteResult route) {
        return new RouteDto(
            route.VehicleId,
            route.Stops.Select(s => s.ToDto()).ToList(),
            route.TotalMinutes,
            route.TotalDistanceKm,
            route.TotalCost,
            VehicleName: null);
    }

    public static TaskAssignmentDto ToDto(this TaskAssignment stop) {
        return new TaskAssignmentDto(
            stop.LocationId,
            stop.ArrivalTime,
            stop.DepartureTime,
            stop.PalletLoad,
            stop.WeightLoad,
            stop.RefrigeratedLoad,
            JobName: null,
            JobType: null,
            CustomerName: null
        );
    }
}
