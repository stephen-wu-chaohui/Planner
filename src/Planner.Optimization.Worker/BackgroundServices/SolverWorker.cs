using Planner.Contracts.Messages;
using Planner.Contracts.Messages.LinearSolver;
using Planner.Messaging;
using Planner.Optimization.LinearSolver;

namespace Planner.Optimization.Worker;

public class SolverWorker(
    IMessageBus bus,
    ILogger<SolverWorker> logger)
    : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[SolverWorker] Starting…");

        try {
            bus.Subscribe<LinearSolverRequestMessage>(
                MessageRoutes.LPSolverRequest,
                async message => {
                    try {
                        logger.LogInformation(
                            "[SolverWorker] Received {Route} RequestId={RequestId}",
                            MessageRoutes.LPSolverRequest,
                            message.RequestId);

                        var response = LinearSolverBuilder.Solve(message.Request);

                        await bus.PublishAsync(
                            MessageRoutes.LPSolverResult,
                            new LinearSolverResultMessage {
                                RequestId = message.RequestId,
                                CompletedAt = DateTime.UtcNow,
                                Response = response
                            });

                        logger.LogInformation(
                            "[SolverWorker] Completed solve for RequestId={RequestId}",
                            message.RequestId);
                    } catch (Exception ex) {
                        logger.LogError(ex,
                            "[SolverWorker] Error processing solve RequestId={RequestId}",
                            message.RequestId);
                    }
                });

            logger.LogInformation(
                "[SolverWorker] Subscribed to route {Route}",
                MessageRoutes.LPSolverRequest);
        } catch (Exception ex) {
            logger.LogCritical(ex,
                "[SolverWorker] FAILED to subscribe to {Route}. Worker will keep running but cannot process messages.",
                MessageRoutes.LPSolverRequest);
        }

        // Keep the background service alive
        while (!stoppingToken.IsCancellationRequested) {
            await Task.Delay(3000, stoppingToken);
        }

        logger.LogInformation("[SolverWorker] Stopping…");
    }
}
