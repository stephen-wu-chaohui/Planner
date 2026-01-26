using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;
using Planner.Contracts.Optimization;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IRouteState
{
    private IReadOnlyList<RouteDto> _routes = [];
    IReadOnlyList<RouteDto> IRouteState.Routes => _routes;

    private IReadOnlyList<MapRoute> _mapRoutes = [];

    public event Action OnRoutesChanged = delegate { };

    public event Action<int> StartWaitingForSolve = delegate { };

    IReadOnlyList<MapRoute> IRouteState.MapRoutes => _mapRoutes;

    private void OnOptimizationCompleted(RoutingResultDto evt)
    {
        if (!string.IsNullOrEmpty(evt.ErrorMessage))
        {
            LastErrorMessage = evt.ErrorMessage;
            _routes = [];
            _mapRoutes = [];
            NotifyStatus();
        }
        else
        {
            LastErrorMessage = null;
            _routes = evt.Routes.ToList();
            BuildMapRoutes();
            OnRoutesChanged?.Invoke();
        }
    }

    private void BuildMapRoutes()
    {
        // 1. Create a lookup map for O(1) access
        var jobMap = _jobs.ToDictionary(job => job.Location.Id, job => job);
        var vehicleMap = _vehicles.ToDictionary(vehicle => vehicle.Id, vehicle => vehicle);

        _mapRoutes = _routes.Select(route => {
            var vehicle = vehicleMap[route.VehicleId];
            return new MapRoute {
                RouteName = vehicle.Name,
                Color = ColourHelper.ColourFromString(vehicle.Name, 0.95, 0.25) ?? "#FF0000",
                Points = route.Stops.Select(stop => {
                    if (TenantInfo != null && stop.LocationId == TenantInfo.MainDepot.Location.Id) {
                        // Depot stop
                        return new CustomerMarker {
                            Lat = TenantInfo.MainDepot.Location.Latitude,
                            Lng = TenantInfo.MainDepot.Location.Longitude,
                            RouteName = vehicle.Name,
                            Arrival = stop.ArrivalTime / 60.0,
                            Departure = stop.DepartureTime / 60.0,
                            PalletLoad = stop.PalletLoad,
                            WeightLoad = stop.WeightLoad,
                            RefrigeratedLoad = stop.RefrigeratedLoad,
                            Color = ColourHelper.ColourFromString(vehicle.Name, 0.95, 0.25) ?? "#FF0000",
                            Label = "Depot",
                            JobType = "Depot"
                        };
                    } else if (jobMap.TryGetValue(stop.LocationId, out var job)) {
                        return new CustomerMarker {
                            Lat = job.Location.Latitude,
                            Lng = job.Location.Longitude,
                            RouteName = vehicle.Name,
                            Arrival = stop.ArrivalTime / 60.0,
                            Departure = stop.DepartureTime / 60.0,
                            PalletLoad = stop.PalletLoad,
                            WeightLoad = stop.WeightLoad,
                            RefrigeratedLoad = stop.RefrigeratedLoad,
                            Color = ColourHelper.ColourFromString(vehicle.Name, 0.95, 0.25) ?? "#FF0000",
                            Label = job.Name,
                            JobType = job.JobType.ToString()
                        };
                    } else {
                        throw new KeyNotFoundException($"Job with LocationId {stop.LocationId} not found.");
                    }
                }).ToList()
            };
        }).ToList();
    }

    public async Task SolveVrpAsync() {
        const string endpoint = "api/vrp/solve";
        var settings = await api.GetFromJsonAsync<OptimizationSummary>(endpoint);

        if (settings?.SearchTimeLimitSeconds > 0) {
            StartWaitingForSolve?.Invoke(settings.SearchTimeLimitSeconds);
        }

        var jobs = await api.GetFromJsonAsync<List<JobDto>>("/api/jobs");
        if (jobs?.Count > 0) {
            _jobs = jobs;
            OnJobsChanged?.Invoke();
        }
    }
}
