using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Planner.API.Services;
using Planner.Application;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
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
                    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                    tenantContext.SetTenant(resp.TenantId);
                    var enrichmentService = scope.ServiceProvider.GetRequiredService<IRouteEnrichmentService>();
                    var firestoreBus = scope.ServiceProvider.GetRequiredService<IFirestoreMessageBus>();
                    var notificationPublisher = scope.ServiceProvider.GetRequiredService<IOptimizationRunNotificationPublisher>();

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

                    var status = string.IsNullOrWhiteSpace(resp.ErrorMessage)
                        ? OptimizationRunStatus.Succeeded
                        : OptimizationRunStatus.Failed;
                    var notification = new OptimizationRunChangedDto(
                        resp.TenantId,
                        resp.OptimizationRunId,
                        Version: 1,
                        status,
                        resp.CompletedAt,
                        new OptimizationRunSummaryDto(
                            JobCount: enrichedDto.Routes.Sum(r => r.Stops.Count),
                            VehicleCount: enrichedDto.Routes.Count,
                            RequestedAtUtc: resp.CompletedAt,
                            SearchTimeLimitSeconds: 0,
                            RequestedBy: null),
                        HasResult: false,
                        HasAiInsight: false,
                        resp.ErrorMessage);

                    await notificationPublisher.PublishRunChangedAsync(notification, stoppingToken);
                    await notificationPublisher.PublishOptimizationCompletedAsync(enrichedDto, stoppingToken);
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
