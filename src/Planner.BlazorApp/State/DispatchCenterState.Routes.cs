using Planner.BlazorApp.Components;
using Planner.BlazorApp.Components.DispatchCenter.Models;
using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;
using Planner.Contracts.Optimization;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IRouteState
{
    private List<RouteDto> _routes = [];
    IReadOnlyList<RouteDto> IRouteState.Routes => _routes.AsReadOnly();

    public OptimizationSummaryInfo LastOptimizationSummary { get; private set; }

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
            _routes = [.. evt.Routes];
            BuildMapRoutes();
            OnRoutesChanged?.Invoke();
        }
        // Calculate optimization summary
        if (_routes != null && _routes.Count > 0) {
            var totalCost = _routes.Sum(r => r.TotalCost);
            var totalStops = _routes.Sum(r => r.Stops.Count);
            LastOptimizationSummary = new OptimizationSummaryInfo(_routes.Count, totalStops, totalCost);
        } else if (string.IsNullOrEmpty(LastErrorMessage)) {
            // Clear summary if no routes and no error (manual reset)
            LastOptimizationSummary = null;
        }

    }

    private void BuildMapRoutes()
    {
        _mapRoutes = [.. _routes.Select(route => {
            var routeName = route.VehicleName ?? "Unknown";
            return new MapRoute {
                RouteName = routeName,
                Color = ColourHelper.ColourFromString(routeName, 0.95, 0.25) ?? "#FF0000",
                Points = [.. route.Stops.Select(stop => {
                    return new CustomerMarker {
                        Lat = stop.Latitute,
                        Lng = stop.Longtitute,
                        RouteName = routeName,
                        Arrival = stop.ArrivalTime / 60.0,
                        Departure = stop.DepartureTime / 60.0,
                        PalletLoad = stop.PalletLoad,
                        WeightLoad = stop.WeightLoad,
                        RefrigeratedLoad = stop.RefrigeratedLoad,
                        Color = ColourHelper.ColourFromString(routeName, 0.95, 0.25) ?? "#FF0000",
                        Label = stop.JobName ?? "Depot",
                        JobType = stop.JobType?.ToString() ?? "Depot"
                    };
                })]
            };
        })];
    }

    public async Task SolveVrpAsync(int? searchTimeLimitSeconds = null) {
        var endpoint = searchTimeLimitSeconds.HasValue 
            ? $"api/vrp/solve?searchTimeLimitSeconds={searchTimeLimitSeconds.Value}"
            : "api/vrp/solve";
        var settings = await api.GetFromJsonAsync<OptimizationSummary>(endpoint);

        if (settings?.SearchTimeLimitSeconds > 0) {
            StartWaitingForSolve?.Invoke(settings.SearchTimeLimitSeconds);
        }

        // ✅ Using REST API endpoint
        var jobs = await api.GetFromJsonAsync<List<JobDto>>("/api/jobs") ?? [];
        if (jobs.Count > 0) {
            _jobs = jobs;
            OnJobsChanged?.Invoke();
        }
    }

    public async Task ClearRoutesAsync() {
        _routes = [];
        _mapRoutes = [];
        OnRoutesChanged?.Invoke();
        await Task.CompletedTask;
    }
}
