using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Planner.Messaging.RabbitMQ;

public class RabbitMqMessageBus(IRabbitMqConnection connection) : IMessageBus {
    public Task PublishAsync<T>(string queueName, T message) {
        using var channel = connection.CreateChannel();
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueuePurge(queueName);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: "",
                             routingKey: queueName,
                             basicProperties: null,
                             body: body);
        Console.WriteLine($"[{DateTime.Now}] RabbitMqMessageBus.PublishAsync({queueName})");
        bool logRabbitMq = Environment.GetEnvironmentVariable("LOG_RABBITMQ") == "true";
        if (logRabbitMq)
            Console.WriteLine($"{json}");
        return Task.CompletedTask;
    }

    public void Subscribe<T>(string queueName, Func<T, Task> onMessage) {
        var channel = connection.CreateChannel();
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        Console.WriteLine($"[{DateTime.Now}] Subscribe.channel.QueueDeclare({queueName})");
        channel.QueuePurge(queueName);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) => {
            try {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($"[{DateTime.Now}] [RabbitMqMessageBus].Received({queueName})");
                bool logRabbitMq = Environment.GetEnvironmentVariable("LOG_RABBITMQ") == "true";
                if (logRabbitMq)
                    Console.WriteLine($"{json}");
                var obj = JsonSerializer.Deserialize<T>(json);
                if (obj != null) {
                    await onMessage(obj);
                }
            } catch (Exception ex) {
                Console.WriteLine($"[{DateTime.Now}] [RabbitMqMessageBus] Error: {ex.Message}");
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: true, consumer);
        Console.WriteLine($"[{DateTime.Now}] [RabbitMqMessageBus].Subscribe({queueName})");
    }
}
