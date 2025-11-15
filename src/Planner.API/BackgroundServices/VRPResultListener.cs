using Planner.Application.Messaging;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Messaging;

namespace Planner.API.BackgroundServices;

public class VRPResultListener(
    IMessageBus bus,
    IMessageHubPublisher hub,
    ILogger<VRPResultListener> logger)
    : BackgroundService {
    private const string QueueName = MessageRoutes.VRPSolverResult;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[VRPResultListener] Starting…");

        try {
            bus.Subscribe<VrpResultMessage>(
                QueueName,
                async result => {
                    try {
                        logger.LogInformation(
                            "[VRPResultListener] Received {QueueName}, forwarding to SignalR (RequestId={RequestId})",
                            QueueName,
                            result.RequestId);

                        await hub.PublishAsync(QueueName, result);
                    } catch (Exception ex) {
                        logger.LogError(ex,
                            "[VRPResultListener] Error forwarding VRP result to SignalR (RequestId={RequestId})",
                            result.RequestId);
                    }
                });

            logger.LogInformation(
                "[VRPResultListener] Subscribed to route {Route}",
                QueueName);
        } catch (Exception ex) {
            logger.LogCritical(ex,
                "[VRPResultListener] FAILED to subscribe to {Route}. Listener will remain alive but cannot process messages.",
                QueueName);
        }

        // Keep alive until API shuts down
        while (!stoppingToken.IsCancellationRequested) {
            await Task.Delay(3000, stoppingToken);
        }

        logger.LogInformation("[VRPResultListener] Stopping…");
    }
}
