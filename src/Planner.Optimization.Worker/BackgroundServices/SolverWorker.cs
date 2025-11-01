using Planner.Messaging;
using Planner.Contracts.Messages;
using Planner.Optimization.LinearSolver;
using Planner.Contracts.Messages.LinearSolver;

namespace Planner.Optimization.Worker;

public class SolverWorker(IMessageBus bus) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bus.Subscribe<LinearSolverRequestMessage>(MessageRoutes.OptimizationRequest, async message => {
            Console.WriteLine($"[SolverWorker].Subscribe({MessageRoutes.OptimizationRequest})!");
            var response = LinearSolverBuilder.Solve(message.Request);

            await bus.PublishAsync(MessageRoutes.OptimizationResult,
                new LinearSolverResultMessage {
                    RequestId = message.RequestId,
                    CompletedAt = DateTime.UtcNow,
                    Response = response
                }
            );
        });
        Console.WriteLine($"[SolverWorker].ExecuteAsync({MessageRoutes.OptimizationRequest}) starts.");
        return Task.CompletedTask;
    }
}
