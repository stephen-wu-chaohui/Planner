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
            // Load tenant metadata first
            await LoadTenantMetadataAsync();

            var vTask = api.GetFromJsonAsync<List<VehicleDto>>("/api/vehicles");
            var cTask = api.GetFromJsonAsync<List<CustomerDto>>("/api/customers");
            var jTask = api.GetFromJsonAsync<List<JobDto>>("/api/jobs");
            await Task.WhenAll(vTask, cTask, jTask);
            _vehicles = vTask.Result ?? [];
            _customers = cTask.Result ?? [];
            _jobs = jTask.Result ?? [];

            await SetMapDepotFromTenantOrVehicles();
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

    public DepotDto ? MainDepot { get; private set; }

    private async Task SetMapDepotFromTenantOrVehicles() {
        // Fetch all depots once
        var depots = await api.GetFromJsonAsync<List<DepotDto>>("api/depots") ?? [];
        
        // Try to use the main depot from tenant metadata first
        if (_tenant?.MainDepotId != null) {
            var depot = depots.FirstOrDefault(d => d.Id == _tenant.MainDepotId);
            if (depot != null) {
                MainDepot = depot;
                return;
            }
        }

        // Fallback: use any depot for map center
        var fallbackDepot = depots.FirstOrDefault();
        if (fallbackDepot is null)
            return;

        MainDepot = fallbackDepot;
    }

}
