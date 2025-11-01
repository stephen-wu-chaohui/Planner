using Microsoft.Extensions.DependencyInjection;
using Planner.Messaging.RabbitMQ;

namespace Planner.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingBus(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        return services;
    }
}
