using Google.Cloud.Firestore;
using Planner.Contracts.Optimization;
using System.Text.Json;

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
public sealed class OptimizationResultsListenerService : IOptimizationResultsListenerService
{
    private readonly FirestoreDb? _db;
    private readonly ILogger<OptimizationResultsListenerService> _logger;
    private readonly bool _isEnabled;
    private FirestoreChangeListener? _listener;

    public event Action<RoutingResultDto>? OnOptimizationCompleted;

    public OptimizationResultsListenerService(
        IConfiguration configuration,
        ILogger<OptimizationResultsListenerService> logger)
    {
        _logger = logger;
        
        var projectId = configuration["Firestore:ProjectId"];
        // Look for the raw JSON string in environment variables
        var base64Json = configuration["FIREBASE_CONFIG_JSON"];

        // Firestore is optional - if not configured, service is disabled
        if (string.IsNullOrEmpty(base64Json)) {
            _logger.LogInformation("Firestore not configured (missing FIREBASE_CONFIG_JSON). Optimization result listener disabled.");
            _isEnabled = false;
            return;
        }

        try {
            // Use FirestoreDbBuilder to avoid setting process-wide environment variables
            string finalJson;
            if (!base64Json.Trim().StartsWith("{")) {
                var data = Convert.FromBase64String(base64Json);
                finalJson = System.Text.Encoding.UTF8.GetString(data);
            } else {
                finalJson = base64Json;
            }
            var builder = new FirestoreDbBuilder {
                ProjectId = projectId,
                JsonCredentials = finalJson
            };
            _db = builder.Build();
            _isEnabled = true;
            _logger.LogInformation("Firestore listener initialized for optimization results");
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to initialize Firestore listener for optimization results.");
            _isEnabled = false;
        }
    }

    public async Task StartListeningAsync()
    {
        if (!_isEnabled || _db == null)
        {
            _logger.LogDebug("Firestore not enabled, skipping optimization results listener start");
            return;
        }

        if (_listener != null)
        {
            _logger.LogDebug("Firestore optimization results listener already running");
            return;
        }

        try
        {
            // Listen to all documents in pending_analysis collection
            var collectionRef = _db.Collection("pending_analysis");
            
            _listener = collectionRef.Listen(snapshot =>
            {
                foreach (var change in snapshot.Changes)
                {
                    if (change.ChangeType == DocumentChange.Type.Added)
                    {
                        try
                        {
                            var doc = change.Document;
                            var data = doc.ToDictionary();
                            
                            // Extract json_payload field which contains the RoutingResultDto
                            if (!data.ContainsKey("json_payload"))
                            {
                                _logger.LogWarning("Document {DocId} missing json_payload field", doc.Id);
                                continue;
                            }

                            var jsonPayload = data["json_payload"]?.ToString();
                            if (string.IsNullOrEmpty(jsonPayload))
                            {
                                _logger.LogWarning("Document {DocId} has empty json_payload", doc.Id);
                                continue;
                            }

                            // Deserialize the json_payload to RoutingResultDto
                            var result = JsonSerializer.Deserialize<RoutingResultDto>(
                                jsonPayload,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (result == null)
                            {
                                _logger.LogWarning("Failed to deserialize json_payload for document {DocId}", doc.Id);
                                continue;
                            }

                            _logger.LogInformation("New optimization result received: Run {OptimizationRunId}", result.OptimizationRunId);
                            OnOptimizationCompleted?.Invoke(result);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing optimization result document {DocId}", change.Document.Id);
                        }
                    }
                }
            });

            _logger.LogInformation("Firestore listener started for pending_analysis collection");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Firestore listener for optimization results");
            throw;
        }
    }

    public async Task StopListeningAsync()
    {
        if (_listener != null)
        {
            await _listener.StopAsync();
            _listener = null;
            _logger.LogInformation("Firestore optimization results listener stopped");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopListeningAsync();
    }
}
