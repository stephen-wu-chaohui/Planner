using Planner.Contracts.Messages;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Messaging;
using Planner.Optimization.VehicleRoutingProblem;

namespace Planner.Optimization.Worker;

public class VRPSolverWorker(
    IMessageBus bus,
    ILogger<VRPSolverWorker> logger)
    : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[VRPSolverWorker] Starting…");

        // Subscribe once, but protect with try/catch
        try {
            bus.Subscribe<VrpRequestMessage>(
                MessageRoutes.VRPSolverRequest,
                async message => {
                    try {
                        logger.LogInformation(
                            "[VRPSolverWorker] Received VRP request {RequestId}",
                            message.RequestId);

                        var response = VrpSolver.Solve(message.Request);

                        await bus.PublishAsync(
                            MessageRoutes.VRPSolverResult,
                            new VrpResultMessage {
                                RequestId = message.RequestId,
                                CompletedAt = DateTime.UtcNow,
                                Result = response
                            });

                        logger.LogInformation(
                            "[VRPSolverWorker] Completed VRP request {RequestId}",
                            message.RequestId);
                    } catch (Exception ex) {
                        logger.LogError(ex,
                            "[VRPSolverWorker] Error processing VRP request {RequestId}",
                            message.RequestId);
                    }
                });

            logger.LogInformation(
                "[VRPSolverWorker] Subscribed to {Route}",
                MessageRoutes.VRPSolverRequest);
        } catch (Exception ex) {
            logger.LogCritical(ex,
                "[VRPSolverWorker] FAILED to subscribe. Worker will still stay alive and retry on restart.");
        }

        // Keep service alive until shutdown
        // Azure Web Apps require your BackgroundService to remain alive
        while (!stoppingToken.IsCancellationRequested) {
            await Task.Delay(3000, stoppingToken);
        }

        logger.LogInformation("[VRPSolverWorker] Stopping…");
    }
}
