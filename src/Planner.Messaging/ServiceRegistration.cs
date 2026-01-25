using Microsoft.Extensions.DependencyInjection;
using Planner.Messaging.Messaging;
using Planner.Messaging.RabbitMQ;

namespace Planner.Messaging.DependencyInjection;

public static class ServiceRegistration {
    /// <summary>
    /// Registers optimization services and implementations.
    /// </summary>
    public static IServiceCollection AddMessagingBus(this IServiceCollection services) {
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        return services;
    }
}
