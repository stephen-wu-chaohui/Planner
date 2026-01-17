using Planner.BlazorApp.State.Interfaces;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : ITenantState {
    public TenantInfo? TenantInfo { get; private set; }

    public event Action OnTenantInfoReady = delegate { };

    public async Task LoadTenantInfo() {
        var tenantInfo = await api.GetFromJsonAsync<TenantInfo>("/api/config/init");

        // Initialize the tenant information
        TenantInfo = tenantInfo;
        OnTenantInfoReady.Invoke();
    }
}
