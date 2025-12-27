using Microsoft.AspNetCore.SignalR;
using Planner.Application.Messaging;
using Planner.Contracts.Optimization.Responses;
using Planner.Messaging;

namespace Planner.API.SignalR;

public class OptimizationResultNotifier : IMessageHubPublisher {
    private readonly IHubContext<PlannerHub> _hubContext;
    private readonly ILogger<OptimizationResultNotifier> _logger;

    public OptimizationResultNotifier(IHubContext<PlannerHub> hubContext, ILogger<OptimizationResultNotifier> logger) {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string method, T message) where T : class {
        try {
            await _hubContext.Clients.All.SendAsync(method, message);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error publishing message to SignalR clients (method={Method})", method);
        }
    }

    public async Task NotifyAsync(OptimizeRouteResponse evt) {
        try {
            await _hubContext.Clients
                .Group(PlannerHub.TenantGroup(evt.TenantId))
                .SendAsync(MessageRoutes.Response, evt);

            await _hubContext.Clients
                .Group(PlannerHub.OptimizationRunGroup(
                    evt.TenantId, evt.OptimizationRunId))
                .SendAsync(MessageRoutes.Response, evt);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error notifying SignalR groups for tenant {Tenant} run {Run}", evt.TenantId, evt.OptimizationRunId);
        }
    }
}

