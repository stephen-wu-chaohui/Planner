using Planner.Application.Messaging;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.LinearSolver;
using Planner.Messaging;

namespace Planner.API.BackgroundServices;

public class LPResultListener(IMessageBus bus, IMessageHubPublisher hub) : BackgroundService {
    private const string busQueue = MessageRoutes.LPSolverResult;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        bus.Subscribe<LinearSolverResultMessage>(busQueue, async result => {
            await hub.PublishAsync(busQueue, result);
        });
        return Task.CompletedTask;
    }
}
