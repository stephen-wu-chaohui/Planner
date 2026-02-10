using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Planner.API.Services;
using Planner.Messaging;
using Planner.Messaging.Firestore;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.API.BackgroundServices;

public sealed class OptimizeRouteResultConsumer(
    IMessageBus bus,
    IServiceScopeFactory scopeFactory,
    ILogger<OptimizeRouteResultConsumer> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("[OptimizeRouteResultConsumer] Starting.");

        using var subscription = bus.Subscribe<OptimizeRouteResponse>(
            MessageRoutes.Response,
            async resp => {
                try {
                    using var scope = scopeFactory.CreateScope();
                    var enrichmentService = scope.ServiceProvider.GetRequiredService<IRouteEnrichmentService>();
                    var firestoreBus = scope.ServiceProvider.GetRequiredService<IFirestoreMessageBus>();

                    // Enrich the raw response with data from the database
                    var enrichedDto = await enrichmentService.EnrichAsync(resp);

                    // Maintain the same structure as the old FirestoreService for AI worker compatibility
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(enrichedDto, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    // Publish the enriched result to Firestore for the UI and AI worker to consume
                    await firestoreBus.PublishAsync(
                        FirestoreCollections.PendingAnalysis,
                        resp.OptimizationRunId.ToString(),
                        new Dictionary<string, object>
                        {
                            { "json_payload", jsonPayload },
                            { "status", "new" },
                            { "timestamp", FieldValue.ServerTimestamp }
                        });
                } catch (Exception ex) {
                    logger.LogError(ex,
                        "[OptimizeRouteResultConsumer] Error forwarding optimization result (RunId={RunId})",
                        resp.OptimizationRunId);
                }
            });


        try {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        } catch (OperationCanceledException) {
            // normal shutdown
        }

        logger.LogInformation("[OptimizeRouteResultConsumer] Stopping.");
    }
}
