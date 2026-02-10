using Planner.Application;
using Planner.Application.Messaging;
using Planner.Contracts.Optimization;

namespace Planner.API.Services;

/// <summary>
/// Publishes routing results to connected SignalR clients.
/// </summary>
public interface IRouteService {
    Task PublishAsync(RoutingResultDto result);
}

/// <summary>
/// Scoped service for broadcasting routing results.
/// </summary>
public sealed class RouteService(
    IMessageHubPublisher hub,
    ITenantContext tenantContext) : IRouteService {
    public async Task PublishAsync(RoutingResultDto result) {
        ArgumentNullException.ThrowIfNull(result);
        EnsureTenantContext(result.TenantId);
        await hub.PublishAsync(nameof(RoutingResultDto), result);
    }

    private void EnsureTenantContext(Guid tenantId) {
        if (!tenantContext.IsSet) {
            tenantContext.SetTenant(tenantId);
            return;
        }

        if (tenantContext.TenantId != tenantId) {
            throw new InvalidOperationException("Tenant mismatch for routing result.");
        }
    }
}
