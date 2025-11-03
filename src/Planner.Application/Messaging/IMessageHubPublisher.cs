namespace Planner.Application.Messaging;

/// <summary>
/// Abstraction for real-time SignalR-based communication.
/// Provides publish-subscribe semantics with typed JSON payloads.
/// </summary>
public interface IMessageHubPublisher {
    Task PublishAsync<T>(string method, T message) where T : class;
}

