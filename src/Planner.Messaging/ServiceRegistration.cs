using Microsoft.Extensions.DependencyInjection;
using Planner.Messaging.Firestore;
using Planner.Messaging.Messaging;
using Planner.Messaging.RabbitMQ;

namespace Planner.Messaging;

public static class ServiceRegistration {
    /// <summary>
    /// Registers optimization services and implementations.
    /// </summary>
    public static IServiceCollection AddMessagingBus(this IServiceCollection services) {
        services.AddSingleton<IFirestoreConnectionFactory, FirestoreConnectionFactory>();
        services.AddSingleton<IFirestoreMessageBus, FirestoreMessageBus>();
       
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        return services;
    }
}
