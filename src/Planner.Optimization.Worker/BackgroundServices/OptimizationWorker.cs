using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planner.Contracts.Optimization.Requests;
using Planner.Messaging;
using Planner.Optimization.Worker.Handlers;

namespace Planner.Optimization.Worker.BackgroundServices;

public sealed class OptimizationWorker(
    IMessageBus bus,
    IServiceProvider serviceProvider) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var subscription = bus.Subscribe<OptimizeRouteRequest>(
            MessageRoutes.Request,
            async request => await HandleRequestAsync(request, stoppingToken)
        );

        try {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        } catch (OperationCanceledException) {
            // normal shutdown
        }
    }

    private async Task HandleRequestAsync(OptimizeRouteRequest request, CancellationToken stoppingToken) {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IOptimizationRequestHandler>();
        await handler.HandleAsync(request, stoppingToken);
    }
}