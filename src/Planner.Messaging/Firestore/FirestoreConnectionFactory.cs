using System;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Planner.Messaging.Firestore;

/// <summary>
/// Provides a centralized way to create FirestoreDb instances.
/// </summary>
public interface IFirestoreConnectionFactory
{
    FirestoreDb? Create();
}

public sealed class FirestoreConnectionFactory(IConfiguration configuration, ILogger<FirestoreConnectionFactory> logger) : IFirestoreConnectionFactory
{
    private FirestoreDb? _db;
    private readonly object _lock = new();

    public FirestoreDb? Create()
    {
        lock (_lock)
        {
            if (_db != null)
            {
                return _db;
            }

            var projectId = configuration["Firestore:ProjectId"];
            var base64Json = configuration["FIREBASE_CONFIG_JSON"];

            if (string.IsNullOrEmpty(base64Json))
            {
                logger.LogInformation("Firestore not configured (missing FIREBASE_CONFIG_JSON). Firestore-based services will be disabled.");
                return null;
            }

            try
            {
                string finalJson;
                if (!base64Json.Trim().StartsWith('{'))
                {
                    var data = Convert.FromBase64String(base64Json);
                    finalJson = System.Text.Encoding.UTF8.GetString(data);
                }
                else
                {
                    finalJson = base64Json;
                }

                if (string.IsNullOrWhiteSpace(projectId))
                {
                    projectId = TryGetProjectIdFromCredentials(finalJson);
                    if (!string.IsNullOrWhiteSpace(projectId))
                    {
                        logger.LogInformation(
                            "Firestore:ProjectId was not set; using project_id from FIREBASE_CONFIG_JSON: {ProjectId}.",
                            projectId);
                    }
                }

                if (string.IsNullOrWhiteSpace(projectId))
                {
                    logger.LogWarning(
                        "Firestore not configured (missing Firestore:ProjectId and project_id in FIREBASE_CONFIG_JSON). Firestore-based services will be disabled.");
                    return null;
                }

                var builder = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    JsonCredentials = finalJson
                };
                _db = builder.Build();
                logger.LogInformation("FirestoreDb instance created successfully for project {ProjectId}.", projectId);
                return _db;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize FirestoreDb instance.");
                return null;
            }
        }
    }

    private static string? TryGetProjectIdFromCredentials(string credentialsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(credentialsJson);
            if (doc.RootElement.TryGetProperty("project_id", out var projectIdElement))
            {
                return projectIdElement.GetString();
            }
        }
        catch
        {
            // Ignored intentionally; caller handles null as "not configured".
        }

        return null;
    }
}
