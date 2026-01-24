using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.Optimization.Outputs;
using Planner.Contracts.Optimization.Requests;
using Planner.Contracts.Optimization.Responses;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IRouteState
{
    private IReadOnlyList<RouteResult> _routes = [];
    IReadOnlyList<RouteResult> IRouteState.Routes => _routes;

    private IReadOnlyList<MapRoute> _mapRoutes = [];

    public event Action OnRoutesChanged = delegate { };

    public event Action<int> StartWait = delegate { };

    IReadOnlyList<MapRoute> IRouteState.MapRoutes => _mapRoutes;

    private void OnOptimizationCompleted(OptimizeRouteResponse evt)
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
        _mapRoutes = _routes.Select(route => new MapRoute
        {
            RouteName = route.VehicleName,
            Color = ColourHelper.ColourFromString(route.VehicleName, 0.95, 0.25) ?? "#FF0000",
            Points = route.Stops.Select(stop =>
                    new CustomerMarker
                    {
                        Lat = stop.Location.Latitude,
                        Lng = stop.Location.Longitude,
                        Label = stop.Name,
                        JobType = stop.JobType.ToString()
                    })
                .ToList()

        }).ToList();
    }

    public async Task SolveVrpAsync() {
        const string endpoint = "api/vrp/solve";
        var settings = await api.GetFromJsonAsync<OptimizationSettings>(endpoint);

        if (settings?.SearchTimeLimitSeconds > 0) {
            int waitMinutes = (settings.SearchTimeLimitSeconds + 59) / 60;
            StartWait?.Invoke(waitMinutes);
        }
    }
}
