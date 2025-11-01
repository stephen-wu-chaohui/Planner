namespace Planner.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(string queueName, T message);
    void Subscribe<T>(string queueName, Func<T, Task> onMessage);
}
