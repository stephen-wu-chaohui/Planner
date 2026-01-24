using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.Optimization;

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
        var settings = await api.GetFromJsonAsync<RouteSettings>(endpoint);

        if (settings?.SearchTimeLimitSeconds > 0) {
            int waitMinutes = (settings.SearchTimeLimitSeconds + 59) / 60;
            StartWait?.Invoke(waitMinutes);
        }
    }
}
