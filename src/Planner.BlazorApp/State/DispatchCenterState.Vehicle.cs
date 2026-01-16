using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IVehicleState {
    private List<VehicleDto> _vehicles = [];
    public IReadOnlyList<VehicleDto> Vehicles => _vehicles;

    public event Action OnVehiclesChanged = delegate { };

    public async Task SaveChangesAsync(IEnumerable<VehicleFormModel> models) {
        // 1. Identify which items actually need work
        var dirtyModels = models.Where(m => m.IsDirty || m.PendingDeletion).ToList();
        if (dirtyModels.Count == 0) return;

        IsProcessing = true;
        NotifyStatus();

        try {
            // 2. Execute the internal bulk logic
            bool success = await InternalBulkUpdateAsync(dirtyModels);

            if (success) {
                // 3. Refresh the facts from the API to ensure total synchronization
                _vehicles = await api.GetFromJsonAsync<List<VehicleDto>>("api/vehicles") ?? [];
                OnVehiclesChanged?.Invoke();
                ClearError();
            }
        } catch (Exception ex) {
            LastErrorMessage = "Failed to update vehicles. Optimization parameters may be out of sync. " + ex.Message;
        } finally {
            IsProcessing = false;
            NotifyStatus();
        }
    }

    private async Task<bool> InternalBulkUpdateAsync(List<VehicleFormModel> dirtyModels) {
        // Here we handle the logic of what to send to the API.
        // You can send them one-by-one or as a single batch if your API supports it.
        foreach (var model in dirtyModels) {
            if (model.PendingDeletion) {
                var resp = await api.DeleteAsync("api/vehicles", model.VehicleId);
                if (!resp.IsSuccessStatusCode) return false;
            } else {
                // Convert FormModel back to a DTO or Request object for the API
                var request = model.ToDto();

                var resp = request.Id == 0
                    ? await api.PostAsJsonAsync("api/vehicles", request)
                    : await api.PutAsJsonAsync("api/vehicles", request);
                if (!resp.IsSuccessStatusCode) return false;
            }
        }
        return true;
    }

}
