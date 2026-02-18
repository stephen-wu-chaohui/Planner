using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : ICustomerState {
    private List<CustomerDto> _customers = [];
    public IReadOnlyList<CustomerDto> Customers => _customers;

    public event Action OnCustomersChanged = delegate { };

    public async Task SaveChangesAsync(IEnumerable<CustomerFormModel> models) {
        var dirtyModels = models.Where(m => m.IsDirty || m.PendingDeletion).ToList();
        if (dirtyModels.Count == 0) return;

        IsProcessing = true;
        NotifyStatus();

        try {
            bool success = await InternalBulkUpdateAsync(dirtyModels);

            if (success) {
                // ✅ Using REST API endpoint
                _customers = await api.GetFromJsonAsync<List<CustomerDto>>("/api/customers") ?? [];
                OnCustomersChanged?.Invoke();
                ClearError();
            }
        } catch (Exception ex) {
            LastErrorMessage = "Failed to update customers. " + ex.Message;
        } finally {
            IsProcessing = false;
            NotifyStatus();
        }
    }

    private async Task<bool> InternalBulkUpdateAsync(List<CustomerFormModel> dirtyModels) {
        foreach (var model in dirtyModels) {
            if (model.PendingDeletion) {
                var resp = await api.DeleteAsync("api/customers", model.CustomerId);
                if (!resp.IsSuccessStatusCode) return false;
            } else {
                var request = model.ToDto();
                var resp = request.CustomerId <= 0
                    ? await api.PostAsJsonAsync("api/customers", request)
                    : await api.PutAsJsonAsync($"api/customers/{request.CustomerId}", request);
                if (!resp.IsSuccessStatusCode) return false;
            }
        }
        return true;
    }
}
