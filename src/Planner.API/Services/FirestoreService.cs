using Google.Cloud.Firestore;
using System.Text.Json;

namespace Planner.API.Services;

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
        var credentialsPath = configuration["Firestore:CredentialsPath"];
        
        // Firestore is optional - if not configured, service is disabled
        if (string.IsNullOrEmpty(projectId))
        {
            _logger.LogInformation("Firestore not configured (missing ProjectId). AI analysis features disabled.");
            _isEnabled = false;
            return;
        }

        try
        {
            // Set credentials if path is provided
            if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
                _logger.LogInformation("Firestore credentials loaded from: {Path}", credentialsPath);
            }

            _db = FirestoreDb.Create(projectId);
            _isEnabled = true;
            _logger.LogInformation("Firestore initialized for project: {ProjectId}", projectId);
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

            var docRef = _db.Collection("pending_analysis").Document(requestId);
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
