using Google.Cloud.Firestore;

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
    Task StartListeningAsync();
    Task StopListeningAsync();
}

/// <summary>
/// Implementation of Firestore listener for route insights.
/// </summary>
public sealed class RouteInsightsListenerService : IRouteInsightsListenerService
{
    private readonly FirestoreDb? _db;
    private readonly ILogger<RouteInsightsListenerService> _logger;
    private readonly bool _isEnabled;
    private FirestoreChangeListener? _listener;

    public event Action<RouteInsight>? OnNewInsight;

    public RouteInsightsListenerService(
        IConfiguration configuration,
        ILogger<RouteInsightsListenerService> logger)
    {
        _logger = logger;
        
        var projectId = configuration["Firestore:ProjectId"];
        var credentialsPath = configuration["Firestore:CredentialsPath"];
        
        // Firestore is optional - if not configured, service is disabled
        if (string.IsNullOrEmpty(projectId))
        {
            _logger.LogInformation("Firestore not configured. Route insights features disabled.");
            _isEnabled = false;
            return;
        }

        try
        {
            // Set credentials if path is provided
            if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
            {
                // Use FirestoreDbBuilder to avoid setting process-wide environment variables
                var builder = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    JsonCredentials = File.ReadAllText(credentialsPath)
                };
                _db = builder.Build();
                _logger.LogInformation("Firestore listener initialized with credentials from file");
            }
            else
            {
                // Use default credentials
                _db = FirestoreDb.Create(projectId);
                _logger.LogInformation("Firestore listener initialized with default credentials");
            }
            _isEnabled = true;
            _logger.LogInformation("Firestore listener initialized for route insights");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firestore listener");
            _isEnabled = false;
        }
    }

    public async Task StartListeningAsync()
    {
        if (!_isEnabled || _db == null)
        {
            _logger.LogDebug("Firestore not enabled, skipping listener start");
            return;
        }

        if (_listener != null)
        {
            _logger.LogDebug("Firestore listener already running");
            return;
        }

        try
        {
            // Listen to all documents in route_insights collection
            var collectionRef = _db.Collection("route_insights");
            
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
                            
                            var insight = new RouteInsight
                            {
                                RequestId = doc.Id,
                                Analysis = data.GetValueOrDefault("analysis")?.ToString() ?? string.Empty,
                                Status = data.GetValueOrDefault("status")?.ToString() ?? string.Empty,
                                Timestamp = data.ContainsKey("timestamp") && data["timestamp"] is Timestamp ts
                                    ? ts.ToDateTime()
                                    : DateTime.UtcNow
                            };

                            _logger.LogInformation("New route insight received: {RequestId}", insight.RequestId);
                            OnNewInsight?.Invoke(insight);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing route insight document");
                        }
                    }
                }
            });

            _logger.LogInformation("Firestore listener started for route_insights collection");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Firestore listener");
            throw;
        }
    }

    public async Task StopListeningAsync()
    {
        if (_listener != null)
        {
            await _listener.StopAsync();
            _listener = null;
            _logger.LogInformation("Firestore listener stopped");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopListeningAsync();
    }
}
