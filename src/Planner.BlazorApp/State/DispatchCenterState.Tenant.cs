using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : ITenantState {
    private TenantDto? _tenant;
    
    /// <summary>
    /// Gets the current tenant metadata.
    /// </summary>
    public TenantDto? Tenant => _tenant;

    /// <summary>
    /// Event triggered when tenant metadata changes.
    /// </summary>
    public event Action OnTenantChanged = delegate { };

    /// <summary>
    /// Retrieves and updates tenant metadata from the API.
    /// </summary>
    public async Task LoadTenantMetadataAsync() {
        IsProcessing = true;
        NotifyStatus();
        
        try {
            _tenant = await api.GetFromJsonAsync<TenantDto>("api/tenants/metadata");
            OnTenantChanged?.Invoke();
            ClearError();
        } catch (Exception ex) {
            LastErrorMessage = "Failed to load tenant metadata. " + ex.Message;
        } finally {
            IsProcessing = false;
            NotifyStatus();
        }
    }
}
