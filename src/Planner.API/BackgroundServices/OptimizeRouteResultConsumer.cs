using Planner.Application.Messaging;
using Planner.Contracts.Optimization.Responses;
using Planner.Messaging;

namespace Planner.API.BackgroundServices;

public sealed class OptimizeRouteResultConsumer(
    IMessageBus bus,
    IMessageHubPublisher hub,
    ILogger<OptimizeRouteResultConsumer> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[OptimizeRouteResultConsumer] Starting.");

        using var subscription = bus.Subscribe<OptimizeRouteResponse>(
            MessageRoutes.Response,
            async resp => {
                try {
                    await hub.PublishAsync(
                        MessageRoutes.Response,
                        resp);
                } catch (Exception ex) {
                    logger.LogError(ex,
                        "[OptimizeRouteResultConsumer] Error forwarding optimization result (RunId={RunId})",
                        resp.OptimizationRunId);
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
