using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.RabbitMQ;
using Planner.Optimization.Worker.Handlers;

namespace Planner.Optimization.Worker.BackgroundServices;

public sealed class OptimizationWorker(
    IMessageBus bus,
    IServiceProvider serviceProvider,
    IConfiguration configuration) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!UseAzureOptimizationDispatch()) {
            PurgeQueues();
        }

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
        var rabbit = serviceProvider.GetRequiredService<IRabbitMqConnection>();
        using var channel = rabbit.CreateChannel();

        channel.QueueDeclare(MessageRoutes.Request, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(MessageRoutes.Response, durable: true, exclusive: false, autoDelete: false, arguments: null);

        channel.QueuePurge(MessageRoutes.Request);
        channel.QueuePurge(MessageRoutes.Response);
    }

    private bool UseAzureOptimizationDispatch() =>
        string.Equals(
            configuration["Optimization:DispatchMode"],
            "AzureServiceBus",
            StringComparison.OrdinalIgnoreCase)
        || string.Equals(
            configuration["OptimizationMessaging:Transport"],
            "ServiceBus",
            StringComparison.OrdinalIgnoreCase);
}
