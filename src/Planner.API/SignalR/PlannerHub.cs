using Microsoft.AspNetCore.SignalR;

namespace Planner.API.SignalR;

public class PlannerHub : Hub {
    /// <summary>
    /// Clients join a tenant-scoped group.
    /// Called immediately after connection.
    /// </summary>
    public async Task JoinTenant(Guid tenantId) {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            TenantGroup(tenantId)
        );
    }

    /// <summary>
    /// Optional: clients can further scope to a single optimization run.
    /// </summary>
    public async Task JoinOptimizationRun(Guid tenantId, Guid optimizationRunId) {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            OptimizationRunGroup(tenantId, optimizationRunId)
        );
    }

    internal static string TenantGroup(Guid tenantId)
        => $"tenant:{tenantId}";

    internal static string OptimizationRunGroup(Guid tenantId, Guid runId)
        => $"tenant:{tenantId}:run:{runId}";
}
