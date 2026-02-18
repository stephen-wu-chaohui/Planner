using Google.Cloud.Firestore;
using Planner.Messaging.Firestore;

namespace Planner.BlazorApp.Services;

/// <summary>
/// Model for route insights data from Firestore.
/// </summary>
public record RouteInsight
{
    public string RequestId { get; init; } = string.Empty;
    public string Analysis { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Service that listens to Firestore for new route insights from AI analysis.
/// </summary>
public interface IRouteInsightsListenerService : IAsyncDisposable
{
    event Action<RouteInsight>? OnNewInsight;
    Task StartListeningAsync(Guid tenantId);
    Task StopListeningAsync();
}

/// <summary>
/// Implementation of Firestore listener for route insights.
/// </summary>
public sealed class RouteInsightsListenerService(
    IFirestoreMessageBus firestoreBus,
    ILogger<RouteInsightsListenerService> logger) : IRouteInsightsListenerService
{
    private FirestoreChangeListener? _listener;

    public event Action<RouteInsight>? OnNewInsight;

    public async Task StartListeningAsync(Guid tenantId)
    {
        if (_listener != null)
        {
            logger.LogDebug("Firestore route insights listener already running");
            return;
        }

        try
        {
            // Use the unified IFirestoreMessageBus to listen to the collection
            _listener = await firestoreBus.SubscribeToCollectionAsync<Dictionary<string, object>>(
                FirestoreCollections.RouteInsights,
                async (data, docId) =>
                {
                    try
                    {
                        var insight = new RouteInsight
                        {
                            RequestId = docId,
                            Analysis = data.GetValueOrDefault("analysis")?.ToString() ?? string.Empty,
                            Status = data.GetValueOrDefault("status")?.ToString() ?? string.Empty,
                            Timestamp = data.ContainsKey("timestamp") && data["timestamp"] is Timestamp ts
                                ? ts.ToDateTime()
                                : DateTime.UtcNow
                        };

                        logger.LogInformation("New route insight received: {RequestId}", insight.RequestId);
                        OnNewInsight?.Invoke(insight);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing route insight document {DocId}", docId);
                    }

                    await Task.CompletedTask;
                });

            logger.LogInformation("Firestore listener started for route_insights collection via IFirestoreMessageBus");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Firestore listener for route insights");
            throw;
        }
    }

    public async Task StopListeningAsync()
    {
        if (_listener != null)
        {
            await _listener.StopAsync();
            _listener = null;
            logger.LogInformation("Firestore route insights listener stopped");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopListeningAsync();
    }
}
