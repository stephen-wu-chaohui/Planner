using Planner.Contracts.Messages;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Messaging;
using Planner.Optimization.VehicleRoutingProblem;

namespace Planner.Optimization.Worker;

public class VRPSolverWorker(IMessageBus bus, ILogger<VRPSolverWorker> logger) : BackgroundService {
    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        bus.Subscribe<VrpRequestMessage>(MessageRoutes.VRPSolverRequest, async message => {
            Console.WriteLine($"[VrpSolver] received {MessageRoutes.VRPSolverRequest}!");
            var response = VrpSolver.Solve(message.Request);

            await bus.PublishAsync(MessageRoutes.VRPSolverResult,
                new VrpResultMessage {
                    RequestId = message.RequestId,
                    CompletedAt = DateTime.UtcNow,
                    Result = response
                }
            );
        });
        Console.WriteLine($"[SolverWorker].ExecuteAsync({MessageRoutes.VRPSolverRequest}) starts.");
        return Task.CompletedTask;
    }
}
