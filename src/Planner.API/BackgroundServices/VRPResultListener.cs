using Planner.Application.Messaging;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Messaging;

namespace Planner.API.BackgroundServices;

public class VRPResultListener(IMessageBus bus, IMessageHubPublisher hub) : BackgroundService
{
    private const string busQueue = MessageRoutes.VRPSolverResult;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bus.Subscribe<VrpResultMessage>(busQueue, async result => {
            await hub.PublishAsync(busQueue, result);
        });
        return Task.CompletedTask;
    }
}
