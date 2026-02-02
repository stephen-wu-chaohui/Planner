using Microsoft.Extensions.DependencyInjection;
using Planner.Application.Messaging;

namespace Planner.Infrastructure.Messaging.RabbitMQ;

public static class ServiceRegistration {
    /// <summary>
    /// Registers RabbitMQ messaging services and implementations.
    /// </summary>
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services) {
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        return services;
    }
}
