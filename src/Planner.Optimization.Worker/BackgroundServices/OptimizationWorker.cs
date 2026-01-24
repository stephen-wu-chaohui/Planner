using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planner.Messaging.Optimization;
using Planner.Messaging;
using Planner.Optimization.Worker.Handlers;
using Planner.Messaging.RabbitMQ;
using Planner.Messaging.Messaging;

namespace Planner.Optimization.Worker.BackgroundServices;

public sealed class OptimizationWorker(
    IMessageBus bus,
    IServiceProvider serviceProvider,
    IRabbitMqConnection rabbit) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        PurgeQueues();

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

    private void PurgeQueues() {
        using var channel = rabbit.CreateChannel();

        channel.QueueDeclare(MessageRoutes.Request, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(MessageRoutes.Response, durable: true, exclusive: false, autoDelete: false, arguments: null);

        channel.QueuePurge(MessageRoutes.Request);
        channel.QueuePurge(MessageRoutes.Response);
    }
}