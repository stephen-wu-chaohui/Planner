using Planner.BlazorApp.FormModels;
using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IJobState {
    private List<JobDto> _jobs = [];
    public IReadOnlyList<JobDto> Jobs => _jobs;

    public event Action OnJobsChanged = delegate { };

    public async Task SaveChangesAsync(IEnumerable<JobFormModel> models) {
        var dirtyModels = models.Where(m => m.IsDirty || m.PendingDeletion).ToList();
        if (dirtyModels.Count == 0) return;

        IsProcessing = true;
        NotifyStatus();

        try {
            bool success = await InternalBulkUpdateAsync(dirtyModels);

            if (success) {
                _jobs = await api.GetFromJsonAsync<List<JobDto>>("api/jobs") ?? [];
                OnJobsChanged?.Invoke();
                ClearError();
            }
        } catch (Exception ex) {
            LastErrorMessage = "Failed to update jobs. " + ex.Message;
        } finally {
            IsProcessing = false;
            NotifyStatus();
        }
    }

    private async Task<bool> InternalBulkUpdateAsync(List<JobFormModel> dirtyModels) {
        foreach (var model in dirtyModels) {
            if (model.PendingDeletion) {
                var resp = await api.DeleteAsync("api/jobs", model.JobId);
                if (!resp.IsSuccessStatusCode) return false;
            } else {
                var request = model.ToDto();
                var resp = request.Id <= 0
                    ? await api.PostAsJsonAsync("api/jobs", request)
                    : await api.PutAsJsonAsync($"api/jobs/{request.Id}", request);
                if (!resp.IsSuccessStatusCode) return false;
            }
        }
        return true;
    }
}
