using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;
using Planner.Contracts.Optimization;
using System.Collections.Generic;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IRouteState
{
    private IReadOnlyList<RouteDto> _routes = [];
    IReadOnlyList<RouteDto> IRouteState.Routes => _routes;

    private IReadOnlyList<MapRoute> _mapRoutes = [];

    public event Action OnRoutesChanged = delegate { };

    public event Action<int> StartWait = delegate { };

    IReadOnlyList<MapRoute> IRouteState.MapRoutes => _mapRoutes;

    private void OnOptimizationCompleted(RoutingResultDto evt)
    {
        _routes = evt.Routes.ToList();
        BuildMapRoutes();
        OnRoutesChanged?.Invoke();
    }

    private void BuildMapRoutes()
    {
        // 1. Create a lookup map for O(1) access
        var jobMap = _jobs.ToDictionary(job => job.Id, job => job);

        _mapRoutes = _routes.Select(route => new MapRoute
        {
            RouteName = route.VehicleName,
            Color = ColourHelper.ColourFromString(route.VehicleName, 0.95, 0.25) ?? "#FF0000",
            Points = route.Stops.Select(stop => {
                if (!jobMap.ContainsKey(stop.JobId)) {
                    throw new KeyNotFoundException($"Job ID {stop.JobId} not found in job map.");
                }
                var job = jobMap[stop.JobId];
                return new CustomerMarker {
                    Lat = job.Location.Latitude,
                    Lng = job.Location.Longitude,
                    RouteName = route.VehicleName,
                    Arrival = stop.ArrivalTime / 60.0,
                    Departure = stop.DepartureTime / 60.0,
                    PalletLoad = stop.PalletLoad,
                    WeightLoad = stop.WeightLoad,
                    RefrigeratedLoad = stop.RefrigeratedLoad,
                    Color = ColourHelper.ColourFromString(route.VehicleName, 0.95, 0.25) ?? "#FF0000",
                    Label = job.Name,
                    JobType = job.JobType.ToString()
                };
            }).ToList()
        }).ToList();
    }

    public async Task SolveVrpAsync() {
        const string endpoint = "api/vrp/solve";
        var settings = await api.GetFromJsonAsync<RouteSettings>(endpoint);

        if (settings?.SearchTimeLimitSeconds > 0) {
            int waitMinutes = (settings.SearchTimeLimitSeconds + 59) / 60;
            StartWait?.Invoke(waitMinutes);
        }

        await api.GetFromJsonAsync<List<JobDto>>("/api/jobs");
    }
}
