using Planner.Contracts.OptimizationRuns;

namespace Planner.Application.OptimizationRuns;

public interface IOptimizationJobQueue {
    Task EnqueueAsync(OptimizationJobMessage message, CancellationToken ct);
    Task<IOptimizationJobEnvelope?> ReceiveOneAsync(CancellationToken ct);
}

public interface IOptimizationJobEnvelope {
    OptimizationJobMessage? Message { get; }
    string MessageId { get; }
    string? CorrelationId { get; }
    int DeliveryCount { get; }
    Exception? DeserializationException { get; }

    Task CompleteAsync(CancellationToken ct);
    Task AbandonAsync(CancellationToken ct);
    Task DeadLetterAsync(string reason, string? errorDescription, CancellationToken ct);
}
