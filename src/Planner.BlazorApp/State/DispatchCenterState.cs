using Planner.BlazorApp.Services;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState(
    PlannerApiClient api,
    PlannerGraphQLService plannerGraphQLService,
    IOptimizationResultsListenerService optimizationResultsListenerService,
    IRouteInsightsListenerService routeInsightsListenerService) : IAsyncDisposable
{
    private bool _listenerServicesStarted;

    public async Task InitializeAsync()
    {
        IsProcessing = true;
        NotifyStatus();
        try
        {
            await LoadTenantInfo();

            var vTask = plannerGraphQLService.GetVehiclesAsync();
            var cTask = plannerGraphQLService.GetCustomersAsync();
            var jTask = plannerGraphQLService.GetJobsAsync();
            await Task.WhenAll(vTask, cTask, jTask);
            _vehicles = vTask.Result;
            _customers = cTask.Result;
            _jobs = jTask.Result;

            OnVehiclesChanged?.Invoke();
            OnCustomersChanged?.Invoke();
            OnJobsChanged?.Invoke();
        }
        finally
        {
            IsProcessing = false;
            NotifyStatus();
        }

        await EnsureListenerServicesStartedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_listenerServicesStarted)
        {
            optimizationResultsListenerService.OnOptimizationCompleted -= OnOptimizationCompleted;
            routeInsightsListenerService.OnNewInsight -= HandleNewInsight;

            await Task.WhenAll(
                optimizationResultsListenerService.StopListeningAsync(),
                routeInsightsListenerService.StopListeningAsync());

            _listenerServicesStarted = false;
        }

        GC.SuppressFinalize(this);
    }

    private async Task EnsureListenerServicesStartedAsync()
    {
        if (_listenerServicesStarted)
        {
            return;
        }

        optimizationResultsListenerService.OnOptimizationCompleted += OnOptimizationCompleted;
        routeInsightsListenerService.OnNewInsight += HandleNewInsight;

        try
        {
            await Task.WhenAll(
                optimizationResultsListenerService.StartListeningAsync(),
                routeInsightsListenerService.StartListeningAsync());

            _listenerServicesStarted = true;
        }
        catch
        {
            optimizationResultsListenerService.OnOptimizationCompleted -= OnOptimizationCompleted;
            routeInsightsListenerService.OnNewInsight -= HandleNewInsight;

            await Task.WhenAll(
                optimizationResultsListenerService.StopListeningAsync(),
                routeInsightsListenerService.StopListeningAsync());

            throw;
        }
    }
}
