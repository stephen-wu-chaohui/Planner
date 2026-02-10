using Google.Cloud.Firestore;
using Planner.Contracts.Optimization;
using System.Text.Json;
using Planner.Messaging.Firestore;

namespace Planner.BlazorApp.Services;

/// <summary>
/// Service that listens to Firestore for new optimization results from the pending_analysis collection.
/// </summary>
public interface IOptimizationResultsListenerService : IAsyncDisposable
{
    event Action<RoutingResultDto>? OnOptimizationCompleted;
    Task StartListeningAsync();
    Task StopListeningAsync();
}

/// <summary>
/// Implementation of Firestore listener for optimization results.
/// Monitors the pending_analysis collection and extracts RoutingResultDto from json_payload.
/// </summary>
public sealed class OptimizationResultsListenerService(
    IFirestoreMessageBus firestoreBus,
    ILogger<OptimizationResultsListenerService> logger) : IOptimizationResultsListenerService
{
    private FirestoreChangeListener? _listener;

    public event Action<RoutingResultDto>? OnOptimizationCompleted;

    public async Task StartListeningAsync()
    {
        if (_listener != null)
        {
            logger.LogDebug("Firestore optimization results listener already running");
            return;
        }

        try
        {
            // Use the unified IFirestoreMessageBus to listen to the collection
            _listener = await firestoreBus.SubscribeToCollectionAsync<Dictionary<string, object>>(
                FirestoreCollections.PendingAnalysis,
                async (data, docId) =>
                {
                    try
                    {
                        // Extract json_payload field which contains the RoutingResultDto
                        if (!data.ContainsKey("json_payload"))
                        {
                            logger.LogWarning("Document {DocId} missing json_payload field", docId);
                            return;
                        }

                        var jsonPayload = data["json_payload"]?.ToString();
                        if (string.IsNullOrEmpty(jsonPayload))
                        {
                            logger.LogWarning("Document {DocId} has empty json_payload", docId);
                            return;
                        }

                        // Deserialize the json_payload to RoutingResultDto
                        var result = JsonSerializer.Deserialize<RoutingResultDto>(
                            jsonPayload,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            logger.LogInformation("New optimization result received: Run {OptimizationRunId}", result.OptimizationRunId);
                            OnOptimizationCompleted?.Invoke(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing optimization result document {DocId}", docId);
                    }

                    await Task.CompletedTask;
                });

            logger.LogInformation("Firestore listener started for pending_analysis collection via IFirestoreMessageBus");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Firestore listener for optimization results");
            throw;
        }
    }

    public async Task StopListeningAsync()
    {
        if (_listener != null)
        {
            await _listener.StopAsync();
            _listener = null;
            logger.LogInformation("Firestore optimization results listener stopped");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopListeningAsync();
    }
}
