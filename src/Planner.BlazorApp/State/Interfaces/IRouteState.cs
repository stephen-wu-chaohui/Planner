using Planner.BlazorApp.FormModels;
using Planner.Contracts.Optimization.Outputs;

namespace Planner.BlazorApp.State.Interfaces;

public interface IRouteState : IDispatchStateProcessing
{
    IReadOnlyList<RouteResult> Routes { get; }
    IReadOnlyList<MapRoute> MapRoutes { get; }
    event Action OnRoutesChanged;
    event Action<int> StartWait;

    Task SolveVrpAsync();
}
