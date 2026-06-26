using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.Infrastructure.OptimizationRuns;

public sealed class CosmosOptimizationRunStore(
    IConfiguration configuration,
    ILogger<CosmosOptimizationRunStore> logger) : IOptimizationRunStore {
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private CosmosClient? _client;
    private Container? _container;

    public async Task<OptimizationRunDocument> CreateAsync(OptimizationRunDocument run, CancellationToken ct) {
        var container = await GetContainerAsync(ct);
        var response = await container.CreateItemAsync(run, PartitionKeyFor(run.TenantId), cancellationToken: ct);
        return response.Resource;
    }

    public async Task<OptimizationRunDocument?> GetAsync(Guid tenantId, Guid runId, CancellationToken ct) {
        try {
            var container = await GetContainerAsync(ct);
            var response = await container.ReadItemAsync<OptimizationRunDocument>(
                runId.ToString(),
                PartitionKeyFor(tenantId),
                cancellationToken: ct);
            return response.Resource;
        } catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return null;
        }
    }

    public Task MarkQueuedAsync(Guid tenantId, Guid runId, CancellationToken ct) =>
        UpdateAsync(
            tenantId,
            runId,
            run => Advance(run, OptimizationRunStatus.Queued, "Optimization job queued.", errorMessage: null),
            ct);

    public async Task<bool> TryStartAttemptAsync(Guid tenantId, Guid runId, OptimizationRunAttemptDto attempt, CancellationToken ct) {
        var started = false;
        await UpdateAsync(
            tenantId,
            runId,
            run => {
                if (IsTerminal(run.Status)) {
                    return run;
                }

                started = true;
                return Advance(
                    run with { Attempts = [.. run.Attempts, attempt] },
                    OptimizationRunStatus.Running,
                    $"Optimization attempt {attempt.AttemptId} started.",
                    errorMessage: null);
            },
            ct);

        return started;
    }

    public Task SaveSolverResultAsync(Guid tenantId, Guid runId, OptimizeRouteResponse result, CancellationToken ct) {
        var status = string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? OptimizationRunStatus.Succeeded
            : OptimizationRunStatus.Failed;

        var message = status == OptimizationRunStatus.Succeeded
            ? "Optimization completed successfully."
            : result.ErrorMessage ?? "Optimization failed.";

        return UpdateAsync(
            tenantId,
            runId,
            run => CompleteLatestAttempt(
                Advance(
                    run with { SolverResult = result },
                    status,
                    message,
                    result.ErrorMessage),
                status,
                result.ErrorMessage),
            ct);
    }

    public Task SaveFailureAsync(Guid tenantId, Guid runId, string errorMessage, OptimizationRunStatus status, CancellationToken ct) =>
        UpdateAsync(
            tenantId,
            runId,
            run => CompleteLatestAttempt(
                Advance(run, status, errorMessage, errorMessage),
                status,
                errorMessage),
            ct);

    public Task SaveAiInsightAsync(Guid tenantId, Guid runId, OptimizationAiInsightDto insight, CancellationToken ct) =>
        UpdateAsync(
            tenantId,
            runId,
            run => Advance(
                run with { AiInsight = insight },
                run.Status,
                "AI insight updated.",
                run.ErrorMessage),
            ct);

    private async Task UpdateAsync(
        Guid tenantId,
        Guid runId,
        Func<OptimizationRunDocument, OptimizationRunDocument> mutate,
        CancellationToken ct) {
        var container = await GetContainerAsync(ct);

        for (var i = 0; i < 3; i++) {
            var response = await container.ReadItemAsync<OptimizationRunDocument>(
                runId.ToString(),
                PartitionKeyFor(tenantId),
                cancellationToken: ct);

            var updated = mutate(response.Resource);
            if (ReferenceEquals(updated, response.Resource) || updated == response.Resource) {
                return;
            }

            try {
                await container.ReplaceItemAsync(
                    updated,
                    updated.Id,
                    PartitionKeyFor(tenantId),
                    new ItemRequestOptions { IfMatchEtag = response.ETag },
                    ct);
                return;
            } catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed) {
                logger.LogWarning(
                    ex,
                    "Concurrent update conflict for optimization run {RunId}; retrying.",
                    runId);
            }
        }

        throw new InvalidOperationException($"Could not update optimization run {runId} after concurrent modification retries.");
    }

    private static OptimizationRunDocument Advance(
        OptimizationRunDocument run,
        OptimizationRunStatus status,
        string message,
        string? errorMessage) {
        var now = DateTime.UtcNow;
        return run with {
            Status = status,
            Version = run.Version + 1,
            UpdatedAtUtc = now,
            ErrorMessage = errorMessage,
            Timeline = [
                .. run.Timeline,
                new OptimizationRunTimelineEventDto(Guid.NewGuid(), status, now, message)
            ]
        };
    }

    private static OptimizationRunDocument CompleteLatestAttempt(
        OptimizationRunDocument run,
        OptimizationRunStatus status,
        string? errorMessage) {
        var attempts = run.Attempts.ToList();
        if (attempts.Count == 0) {
            return run;
        }

        var latest = attempts[^1];
        attempts[^1] = latest with {
            CompletedAtUtc = DateTime.UtcNow,
            Status = status,
            ErrorMessage = errorMessage
        };

        return run with { Attempts = attempts };
    }

    private async Task<Container> GetContainerAsync(CancellationToken ct) {
        if (_container is not null) {
            return _container;
        }

        await _initLock.WaitAsync(ct);
        try {
            if (_container is not null) {
                return _container;
            }

            var databaseName = configuration["Cosmos:DatabaseName"] ?? "planner";
            var containerName = configuration["Cosmos:OptimizationRunsContainerName"] ?? "optimizationRuns";
            _client = CreateClient(configuration);
            var database = await _client.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: ct);
            var containerResponse = await database.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(containerName, "/tenantId"),
                cancellationToken: ct);

            _container = containerResponse.Container;
            return _container;
        } finally {
            _initLock.Release();
        }
    }

    private static CosmosClient CreateClient(IConfiguration configuration) {
        var options = CreateClientOptions(configuration);
        var connectionString = configuration["Cosmos:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString)) {
            return new CosmosClient(connectionString, options);
        }

        var endpoint = configuration["Cosmos:Endpoint"];
        var key = configuration["Cosmos:Key"];
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key)) {
            throw new InvalidOperationException("Cosmos configuration requires Cosmos:ConnectionString or Cosmos:Endpoint plus Cosmos:Key.");
        }

        return new CosmosClient(endpoint, key, options);
    }

    private static CosmosClientOptions CreateClientOptions(IConfiguration configuration) {
        var options = new CosmosClientOptions();
        if (Enum.TryParse<ConnectionMode>(configuration["Cosmos:ConnectionMode"], ignoreCase: true, out var connectionMode)) {
            options.ConnectionMode = connectionMode;
        }

        if (TryReadPositiveSeconds(configuration["Cosmos:RequestTimeoutSeconds"], out var requestTimeout)) {
            options.RequestTimeout = requestTimeout;
        }

        if (bool.TryParse(configuration["Cosmos:DisableServerCertificateValidation"], out var disabled) && disabled) {
            options.HttpClientFactory = () => new HttpClient(new HttpClientHandler {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }

        return options;
    }

    private static bool TryReadPositiveSeconds(string? value, out TimeSpan timeout) {
        timeout = default;
        if (!int.TryParse(value, out var seconds) || seconds <= 0) {
            return false;
        }

        timeout = TimeSpan.FromSeconds(seconds);
        return true;
    }

    private static PartitionKey PartitionKeyFor(Guid tenantId) => new(tenantId.ToString());

    private static bool IsTerminal(OptimizationRunStatus status) =>
        status is OptimizationRunStatus.Succeeded
            or OptimizationRunStatus.Failed
            or OptimizationRunStatus.DeadLettered
            or OptimizationRunStatus.Cancelled;
}
