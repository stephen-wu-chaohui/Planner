using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;

namespace Planner.Infrastructure.ServiceBus;

public sealed class ServiceBusOptimizationJobQueue(IConfiguration configuration) : IOptimizationJobQueue, IAsyncDisposable {
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private ServiceBusClient? _client;

    public async Task EnqueueAsync(OptimizationJobMessage message, CancellationToken ct) {
        await using var sender = GetClient().CreateSender(GetQueueName());

        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(message, _jsonOptions)) {
            MessageId = message.OptimizationRunId.ToString(),
            CorrelationId = message.OptimizationRunId.ToString(),
            ContentType = "application/json",
            Subject = nameof(OptimizationJobMessage)
        };
        serviceBusMessage.ApplicationProperties["tenantId"] = message.TenantId.ToString();
        serviceBusMessage.ApplicationProperties["optimizationRunId"] = message.OptimizationRunId.ToString();

        await sender.SendMessageAsync(serviceBusMessage, ct);
    }

    public async Task<IOptimizationJobEnvelope?> ReceiveOneAsync(CancellationToken ct) {
        var receiver = GetClient().CreateReceiver(
            GetQueueName(),
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });

        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), ct);
        if (message is null) {
            await receiver.DisposeAsync();
            return null;
        }

        OptimizationJobMessage? jobMessage = null;
        Exception? deserializationException = null;
        try {
            jobMessage = message.Body.ToObjectFromJson<OptimizationJobMessage>(_jsonOptions);
        } catch (Exception ex) {
            deserializationException = ex;
        }

        return new ServiceBusOptimizationJobEnvelope(receiver, message, jobMessage, deserializationException);
    }

    public async ValueTask DisposeAsync() {
        if (_client is not null) {
            await _client.DisposeAsync();
        }
    }

    private ServiceBusClient GetClient() {
        if (_client is not null) {
            return _client;
        }

        var connectionString = configuration["ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) {
            throw new InvalidOperationException("ServiceBus:ConnectionString is not configured.");
        }

        _client = new ServiceBusClient(connectionString);
        return _client;
    }

    private string GetQueueName() =>
        configuration["ServiceBus:OptimizationQueueName"] ?? "optimization-jobs";

    private sealed class ServiceBusOptimizationJobEnvelope(
        ServiceBusReceiver receiver,
        ServiceBusReceivedMessage receivedMessage,
        OptimizationJobMessage? message,
        Exception? deserializationException) : IOptimizationJobEnvelope {

        public OptimizationJobMessage? Message { get; } = message;
        public string MessageId => receivedMessage.MessageId;
        public string? CorrelationId => receivedMessage.CorrelationId;
        public int DeliveryCount => receivedMessage.DeliveryCount;
        public Exception? DeserializationException { get; } = deserializationException;

        public async Task CompleteAsync(CancellationToken ct) {
            await receiver.CompleteMessageAsync(receivedMessage, ct);
            await receiver.DisposeAsync();
        }

        public async Task AbandonAsync(CancellationToken ct) {
            await receiver.AbandonMessageAsync(receivedMessage, cancellationToken: ct);
            await receiver.DisposeAsync();
        }

        public async Task DeadLetterAsync(string reason, string? errorDescription, CancellationToken ct) {
            await receiver.DeadLetterMessageAsync(
                receivedMessage,
                deadLetterReason: reason,
                deadLetterErrorDescription: errorDescription,
                cancellationToken: ct);
            await receiver.DisposeAsync();
        }
    }
}
