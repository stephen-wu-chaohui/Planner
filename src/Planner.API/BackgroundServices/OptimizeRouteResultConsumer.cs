using Planner.Application.Messaging;
using Planner.Contracts.Messaging.Events;
using Planner.Contracts.Realtime;
using Planner.Messaging;

namespace Planner.API.BackgroundServices;

public sealed class OptimizeRouteResultConsumer(
    IMessageBus bus,
    IMessageHubPublisher hub,
    ILogger<OptimizeRouteResultConsumer> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[OptimizeRouteResultConsumer] Starting.");

        using var subscription = bus.Subscribe<RouteOptimizedEvent>(
            SignalRMethods.OptimizationCompleted,
            async evt => {
                try {
                    await hub.PublishAsync(
                        SignalRMethods.OptimizationCompleted,
                        evt);
                } catch (Exception ex) {
                    logger.LogError(ex,
                        "[OptimizeRouteResultConsumer] Error forwarding optimization result (RunId={RunId})",
                        evt.OptimizationRunId);
                }
            });

        try {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        } catch (OperationCanceledException) {
            // normal shutdown
        }

        logger.LogInformation("[OptimizeRouteResultConsumer] Stopping.");
    }
}
