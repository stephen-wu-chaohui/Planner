using Google.Cloud.Firestore;
using System.Text.Json;

namespace Planner.API.Services;

/// <summary>
/// Shared Firestore collection names used across the application.
/// </summary>
public static class FirestoreCollections
{
    /// <summary>
    /// Collection for optimization results pending AI analysis.
    /// Also used by BlazorApp to receive real-time optimization results.
    /// </summary>
    public const string PendingAnalysis = "pending_analysis";
    
    /// <summary>
    /// Collection for AI-generated route insights.
    /// </summary>
    public const string RouteInsights = "route_insights";
}

/// <summary>
/// Service for writing optimization results to Firestore for AI analysis.
/// </summary>
public interface IFirestoreService
{
    Task PublishForAnalysisAsync(string requestId, object data);
}

/// <summary>
/// Implementation of Firestore service for writing pending analysis documents.
/// </summary>
public sealed class FirestoreService : IFirestoreService
{
    private readonly FirestoreDb? _db;
    private readonly ILogger<FirestoreService> _logger;
    private readonly bool _isEnabled;

    public FirestoreService(IConfiguration configuration, ILogger<FirestoreService> logger)
    {
        _logger = logger;
        
        var projectId = configuration["Firestore:ProjectId"];
        // Look for the raw JSON string in environment variables
        var base64Json = configuration["FIREBASE_CONFIG_JSON"];

        // Firestore is optional - if not configured, service is disabled
        if (string.IsNullOrEmpty(base64Json))
        {
            _logger.LogInformation("Firestore not configured (missing FIREBASE_CONFIG_JSON). AI analysis features disabled.");
            _isEnabled = false;
            return;
        }

        try
        {
            // Use FirestoreDbBuilder to avoid setting process-wide environment variables
            string finalJson;
            if (!base64Json.Trim().StartsWith("{")) {
                var data = Convert.FromBase64String(base64Json);
                finalJson = System.Text.Encoding.UTF8.GetString(data);
            } else {
                finalJson = base64Json;
            }
            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                JsonCredentials = finalJson
            };
            _db = builder.Build();
            _isEnabled = true;
            _logger.LogInformation("Firestore initialized with credentials from FIREBASE_CONFIG_JSON");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firestore. AI analysis features will be disabled.");
            _isEnabled = false;
        }
    }

    public async Task PublishForAnalysisAsync(string requestId, object data)
    {
        if (!_isEnabled || _db == null)
        {
            _logger.LogDebug("Firestore not enabled, skipping publish for request {RequestId}", requestId);
            return;
        }

        try
        {
            // Serialize data to JSON string
            var jsonPayload = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var document = new Dictionary<string, object>
            {
                { "json_payload", jsonPayload },
                { "status", "new" },
                { "timestamp", FieldValue.ServerTimestamp }
            };

            var docRef = _db.Collection(FirestoreCollections.PendingAnalysis).Document(requestId);
            await docRef.SetAsync(document);

            _logger.LogInformation("Published optimization result to Firestore for AI analysis: {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            // Log but don't throw - Firestore publishing is non-critical
            _logger.LogError(ex, "Failed to publish to Firestore for request {RequestId}", requestId);
        }
    }
}
