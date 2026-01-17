using Planner.BlazorApp.Services;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState(
    PlannerApiClient api,
    IOptimizationHubClient hub)
{
    public async Task InitializeAsync() {
        IsProcessing = true;
        NotifyStatus();
        try {
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
        } finally {
            IsProcessing = false;
            NotifyStatus();
        }

        await hub.ConnectAsync();
        hub.OptimizationCompleted += OnOptimizationCompleted;
    }

}
