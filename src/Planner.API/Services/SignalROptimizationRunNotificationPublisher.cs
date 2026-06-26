using Microsoft.AspNetCore.SignalR;
using Planner.API.Hubs;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;

namespace Planner.API.Services;

public sealed class SignalROptimizationRunNotificationPublisher(
    IHubContext<PlannerHub> hubContext,
    ILogger<SignalROptimizationRunNotificationPublisher> logger) : IOptimizationRunNotificationPublisher {

    public async Task PublishRunChangedAsync(OptimizationRunChangedDto notification, CancellationToken ct) {
        try {
            await hubContext.Clients
                .Group(PlannerHub.TenantGroup(notification.TenantId))
                .SendAsync("optimizationRunChanged", notification, ct);

            logger.LogDebug(
                "Published optimization run notification for tenant {TenantId}, run {RunId}, status {Status}.",
                notification.TenantId,
                notification.OptimizationRunId,
                notification.Status);
        } catch (Exception ex) when (!ct.IsCancellationRequested) {
            logger.LogWarning(
                ex,
                "Failed to publish optimization run notification for tenant {TenantId}, run {RunId}.",
                notification.TenantId,
                notification.OptimizationRunId);
        }
    }

    public async Task PublishInsightChangedAsync(OptimizationInsightChangedDto notification, CancellationToken ct) {
        try {
            await hubContext.Clients
                .Group(PlannerHub.TenantGroup(notification.TenantId))
                .SendAsync("optimizationInsightChanged", notification, ct);
        } catch (Exception ex) when (!ct.IsCancellationRequested) {
            logger.LogWarning(
                ex,
                "Failed to publish optimization insight notification for tenant {TenantId}, run {RunId}.",
                notification.TenantId,
                notification.OptimizationRunId);
        }
    }

    public async Task PublishOptimizationCompletedAsync(RoutingResultDto result, CancellationToken ct) {
        try {
            await hubContext.Clients
                .Group(PlannerHub.TenantGroup(result.TenantId))
                .SendAsync("optimizationCompleted", result, ct);
        } catch (Exception ex) when (!ct.IsCancellationRequested) {
            logger.LogWarning(
                ex,
                "Failed to publish optimization completed notification for tenant {TenantId}, run {RunId}.",
                result.TenantId,
                result.OptimizationRunId);
        }
    }
}
