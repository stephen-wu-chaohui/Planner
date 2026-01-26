using Microsoft.Extensions.DependencyInjection;
using Planner.API.Services;
using Planner.Messaging;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.API.BackgroundServices;

public sealed class OptimizeRouteResultConsumer(
    IMessageBus bus,
    IServiceScopeFactory scopeFactory,
    ILogger<OptimizeRouteResultConsumer> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[OptimizeRouteResultConsumer] Starting.");

        using var subscription = bus.Subscribe<OptimizeRouteResponse>(
            MessageRoutes.Response,
            async resp => {
                try {
                    using var scope = scopeFactory.CreateScope();
                    var routeService = scope.ServiceProvider.GetRequiredService<IRouteService>();
                    await routeService.PublishAsync(resp.ToDto());
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
