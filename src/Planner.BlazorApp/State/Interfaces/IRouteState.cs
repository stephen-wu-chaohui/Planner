using Planner.BlazorApp.FormModels;
using Planner.Contracts.Optimization;

namespace Planner.BlazorApp.State.Interfaces;

public interface IRouteState : IDispatchStateProcessing
{
    IReadOnlyList<RouteDto> Routes { get; }
    IReadOnlyList<MapRoute> MapRoutes { get; }
    OptimizationSummaryInfo LastOptimizationSummary { get; }
    event Action OnRoutesChanged;
    event Action<int> StartWaitingForSolve;

    Task SolveVrpAsync(int? searchTimeLimitSeconds = null);
    Task ClearRoutesAsync();
}

public record OptimizationSummaryInfo(int RouteCount, int StopCount, double TotalCost) {
    public string Text => $"{RouteCount} routes, {StopCount} stops, Total cost: ${TotalCost:N2}";
}

