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
            var vTask = api.GetFromJsonAsync<List<VehicleDto>>("/api/vehicles");
            var cTask = api.GetFromJsonAsync<List<CustomerDto>>("/api/customers");
            var jTask = api.GetFromJsonAsync<List<JobDto>>("/api/jobs");
            await Task.WhenAll(vTask, cTask, jTask);
            _vehicles = vTask.Result ?? [];
            _customers = cTask.Result ?? [];
            _jobs = jTask.Result ?? [];

            SetMapDepotFromVehicles();
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

    public LocationDto? MapCenter { get; private set; }

    private async void SetMapDepotFromVehicles() {
        // Vehicles DTOs do not include depot navigation; fetch a depot for map center.
        var depots = await api.GetFromJsonAsync<List<DepotDto>>("api/depots") ?? [];
        var depot = depots.FirstOrDefault();
        if (depot is null)
            return;

        MapCenter = depot.Location;
    }

}
