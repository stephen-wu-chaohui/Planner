using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace Planner.Messaging.RabbitMQ;

public class RabbitMqMessageBus(ILogger<RabbitMqMessageBus> logger, IRabbitMqConnection connection) : IMessageBus {
    public Task PublishAsync<T>(string queueName, T message) {
        using var channel = connection.CreateChannel();
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: "",
                             routingKey: queueName,
                             basicProperties: null,
                             body: body);
        logger.LogInformation("RabbitMqMessageBus.PublishAsync({QueueName})", queueName);

        var logRabbitMq = Environment.GetEnvironmentVariable("LOG_RABBITMQ") == "true";
        if (logRabbitMq)
            logger.LogInformation("RabbitMqMessageBus.PublishAsync payload: {Payload}", json);
        return Task.CompletedTask;
    }

    public IDisposable Subscribe<T>(string queueName, Func<T, Task> onMessage) {
        var channel = connection.CreateChannel();
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        logger.LogInformation("Subscribe.channel.QueueDeclare({QueueName})", queueName);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) => {
            var logRabbitMq = Environment.GetEnvironmentVariable("LOG_RABBITMQ") == "true";
            try {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                if (logRabbitMq)
                    logger.LogInformation("[RabbitMqMessageBus].Received({QueueName}) {Payload}", queueName, json);

                var obj = JsonSerializer.Deserialize<T>(json);
                if (obj != null)
                    await onMessage(obj);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            } catch (Exception ex) {
                logger.LogError(ex, "[RabbitMqMessageBus] Error processing queue {QueueName}", queueName);
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        var consumerTag = channel.BasicConsume(queue: queueName, autoAck: false, consumer);
        logger.LogInformation("[RabbitMqMessageBus].Subscribe({QueueName})", queueName);

        return new Disposable(() =>
        {
            channel.BasicCancel(consumerTag);
            channel.Dispose();
        });
    }

    class Disposable(Action dispose) : IDisposable {
        public void Dispose() => dispose();
    }
}
