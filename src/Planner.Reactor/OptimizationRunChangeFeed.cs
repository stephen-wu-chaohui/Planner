using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Planner.Contracts.OptimizationRuns;

namespace Planner.Reactor;

public sealed class OptimizationRunChangeFeed(
    IOptimizationRunNotifier notifier,
    ILogger<OptimizationRunChangeFeed> logger) {

    [Function(nameof(OptimizationRunChangeFeed))]
    public async Task RunAsync(
        [CosmosDBTrigger(
            databaseName: "%Cosmos:DatabaseName%",
            containerName: "%Cosmos:OptimizationRunsContainerName%",
            Connection = "Cosmos:ConnectionString",
            LeaseContainerName = "optimizationRunLeases",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<OptimizationRunDocument> documents,
        CancellationToken ct) {

        if (documents.Count == 0) {
            return;
        }

        foreach (var document in documents) {
            logger.LogInformation(
                "Publishing optimization run change notification for run {RunId} status {Status}.",
                document.OptimizationRunId,
                document.Status);

            await notifier.SendRunChangedAsync(document.ToRunChangedDto(), ct);

            if (document.AiInsight is not null) {
                await notifier.SendInsightChangedAsync(document.ToInsightChangedDto(), ct);
            }
        }
    }
}
