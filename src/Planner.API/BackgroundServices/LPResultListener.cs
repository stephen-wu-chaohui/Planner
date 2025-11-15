using Planner.Application.Messaging;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.LinearSolver;
using Planner.Messaging;

namespace Planner.API.BackgroundServices;

public class LPResultListener(
    IMessageBus bus,
    IMessageHubPublisher hub,
    ILogger<LPResultListener> logger)
    : BackgroundService {
    private const string QueueName = MessageRoutes.LPSolverResult;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[LPResultListener] Starting…");

        try {
            bus.Subscribe<LinearSolverResultMessage>(
                QueueName,
                async result => {
                    try {
                        logger.LogInformation(
                            "[LPResultListener] Received {QueueName}, forwarding to SignalR (RequestId={RequestId})",
                            QueueName,
                            result.RequestId);

                        await hub.PublishAsync(QueueName, result);
                    } catch (Exception ex) {
                        logger.LogError(ex,
                            "[LPResultListener] Error forwarding LP result to SignalR (RequestId={RequestId})",
                            result.RequestId);
                    }
                });

            logger.LogInformation(
                "[LPResultListener] Subscribed to route {Route}",
                QueueName);
        } catch (Exception ex) {
            logger.LogCritical(ex,
                "[LPResultListener] FAILED to subscribe to {Route}. Listener will keep running but cannot process messages.",
                QueueName);
        }

        // Keep the listener alive until API shuts down
        while (!stoppingToken.IsCancellationRequested) {
            await Task.Delay(3000, stoppingToken);
        }

        logger.LogInformation("[LPResultListener] Stopping…");
    }
}
