using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Planner.API.SignalR;

[Authorize]
public class PlannerHub : Hub {
    public override async Task OnConnectedAsync() {
        var tenantId = GetTenantId();
        if (tenantId != Guid.Empty) {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Clients join a tenant-scoped group.
    /// Called immediately after connection.
    /// </summary>
    [Obsolete("Tenant group is joined automatically on connection")]
    public Task JoinTenant(Guid tenantId) {
        // No-op or validation
        return Task.CompletedTask;
    }

    /// <summary>
    /// Optional: clients can further scope to a single optimization run.
    /// </summary>
    public async Task JoinOptimizationRun(Guid optimizationRunId) {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty) return;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            OptimizationRunGroup(tenantId, optimizationRunId)
        );
    }

    private Guid GetTenantId() {
        var claim = Context.User?.FindFirst("tenant_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }

    internal static string TenantGroup(Guid tenantId)
        => $"tenant:{tenantId}";

    internal static string OptimizationRunGroup(Guid tenantId, Guid runId)
        => $"tenant:{tenantId}:run:{runId}";
}
