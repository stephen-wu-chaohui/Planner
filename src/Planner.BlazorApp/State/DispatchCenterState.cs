using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Planner.BlazorApp.Services;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState(
    PlannerApiClient api,
    IOptimizationResultsListenerService optimizationResultsListenerService) : IAsyncDisposable
{
    private bool _listenerServicesStarted;

    public async Task InitializeAsync()
    {
        IsProcessing = true;
        NotifyStatus();
        try
        {
            await LoadTenantInfo();

            var vTask = api.GetFromJsonAsync<List<VehicleDto>>("/api/vehicles");
            var cTask = api.GetFromJsonAsync<List<CustomerDto>>("/api/customers");
            var jTask = api.GetFromJsonAsync<List<JobDto>>("/api/jobs");

            await Task.WhenAll(vTask, cTask, jTask);

            _vehicles = vTask.Result ?? [];
            _customers = cTask.Result ?? [];
            _jobs = jTask.Result ?? [];

            OnVehiclesChanged?.Invoke();
            OnCustomersChanged?.Invoke();
            OnJobsChanged?.Invoke();
        }
        catch (UnauthorizedAccessException) {
            throw;
        }
        catch (AccessTokenNotAvailableException) {
            throw;
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
            optimizationResultsListenerService.OnOptimizationRunChanged -= OnOptimizationRunChanged;
            optimizationResultsListenerService.OnOptimizationCompleted -= OnOptimizationCompleted;

            await optimizationResultsListenerService.StopListeningAsync();

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

        optimizationResultsListenerService.OnOptimizationRunChanged += OnOptimizationRunChanged;
        optimizationResultsListenerService.OnOptimizationCompleted += OnOptimizationCompleted;

        try
        {
            Guid currentTenantId = TenantInfo?.TenantId ?? Guid.Empty;
            if (currentTenantId != Guid.Empty) {
                await optimizationResultsListenerService.StartListeningAsync(currentTenantId);

                _listenerServicesStarted = true;
            }
        }
        catch
        {
            optimizationResultsListenerService.OnOptimizationRunChanged -= OnOptimizationRunChanged;
            optimizationResultsListenerService.OnOptimizationCompleted -= OnOptimizationCompleted;

            await optimizationResultsListenerService.StopListeningAsync();

            throw;
        }
    }
}
