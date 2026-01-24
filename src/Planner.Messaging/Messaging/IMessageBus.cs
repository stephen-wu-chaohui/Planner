namespace Planner.Messaging.Messaging;

public interface IMessageBus {
    Task PublishAsync<T>(string queueName, T message);
    IDisposable Subscribe<T>(string queueName, Func<T, Task> onMessage);
}
