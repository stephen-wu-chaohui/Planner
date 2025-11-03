namespace Planner.Application.Messaging;

/// <summary>
/// Abstraction for real-time SignalR-based communication.
/// Provides publish-subscribe semantics with typed JSON payloads.
/// </summary>
public interface IMessageHubClient {
    Task SubscribeAsync<T>(string method, Action<T> handler) where T : class;
}

