using Planner.BlazorApp.Services;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState(
    PlannerApiClient api,
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

            // ✅ Using REST API endpoints
            var vTask = api.GetFromJsonAsync<List<VehicleDto>>("/api/vehicles");
            var cTask = api.GetFromJsonAsync<List<CustomerDto>>("/api/customers");
            var jTask = api.GetFromJsonAsync<List<JobDto>>("/api/jobs");

            await Task.WhenAll(vTask, cTask, jTask);

            _vehicles = vTask.Result;
            _customers = cTask.Result;
            _jobs = jTask.Result;

            OnVehiclesChanged?.Invoke();
            OnCustomersChanged?.Invoke();
            OnJobsChanged?.Invoke();
        } 
        catch (Exception ex) {
            Console.WriteLine($"API FETCH FAILED: {ex.Message}");
            if (ex.InnerException != null) {
                Console.WriteLine($"INNER: {ex.InnerException.Message}");
            }
        }
        finally {
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
            Guid currentTenantId = TenantInfo?.TenantId ?? Guid.Empty;
            if (currentTenantId != Guid.Empty) {
                await Task.WhenAll(
                    optimizationResultsListenerService.StartListeningAsync(currentTenantId!),
                    routeInsightsListenerService.StartListeningAsync(currentTenantId!));

                _listenerServicesStarted = true;
            }
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
