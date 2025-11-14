using Planner.Contracts.Messages;
using Planner.Contracts.Messages.LinearSolver;
using Planner.Messaging;
using Planner.Optimization.LinearSolver;

namespace Planner.Optimization.Worker;

public class SolverWorker(IMessageBus bus) : BackgroundService {
    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        bus.Subscribe<LinearSolverRequestMessage>(MessageRoutes.LPSolverRequest, async message => {
            Console.WriteLine($"[SolverWorker] received {MessageRoutes.LPSolverRequest}!");
            var response = LinearSolverBuilder.Solve(message.Request);

            await bus.PublishAsync(MessageRoutes.LPSolverResult,
                new LinearSolverResultMessage {
                    RequestId = message.RequestId,
                    CompletedAt = DateTime.UtcNow,
                    Response = response
                }
            );
        });
        Console.WriteLine($"[SolverWorker] started {MessageRoutes.LPSolverRequest}.");
        return Task.CompletedTask;
    }
}
