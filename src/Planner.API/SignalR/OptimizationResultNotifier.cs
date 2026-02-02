using Microsoft.AspNetCore.SignalR;
using Planner.Application.Messaging;
using Planner.Contracts.Optimization;
using Planner.Application.Messaging;

namespace Planner.API.SignalR;

public class OptimizationResultNotifier(IHubContext<PlannerHub> hubContext, ILogger<OptimizationResultNotifier> logger) : IMessageHubPublisher {
    public async Task PublishAsync<T>(string method, T message) where T : class {
        try {
            await hubContext.Clients.All.SendAsync(method, message);
        } catch (Exception ex) {
            logger.LogError(ex, "Error publishing message to SignalR clients (method={Method})", method);
        }
    }

    public async Task NotifyAsync(RoutingResultDto evt) {
        try {
            await hubContext.Clients
                .Group(PlannerHub.TenantGroup(evt.TenantId))
                .SendAsync(MessageRoutes.Response, evt);

            await hubContext.Clients
                .Group(PlannerHub.OptimizationRunGroup(
                    evt.TenantId, evt.OptimizationRunId))
                .SendAsync(MessageRoutes.Response, evt);
        } catch (Exception ex) {
            logger.LogError(ex, "Error notifying SignalR groups for tenant {Tenant} run {Run}", evt.TenantId, evt.OptimizationRunId);
        }
    }
}

