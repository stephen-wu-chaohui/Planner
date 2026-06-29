using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.Infrastructure.ServiceBus;

public sealed class AzureOptimizationMessageBus(
    IOptimizationJobQueue jobQueue,
    IOptimizationRunStore runStore,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AzureOptimizationMessageBus> logger) : IMessageBus {

    public const string HttpClientName = "PlannerOptimizationResultApi";
    private const string WorkerResultApiKeyHeader = "X-Optimization-Worker-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task PublishAsync<T>(string queueName, T message) {
        if (queueName == MessageRoutes.Request) {
            return PublishRequestAsync(message);
        }

        if (queueName == MessageRoutes.Response) {
            return PublishResponseAsync(message);
        }

        throw new NotSupportedException(
            $"Azure optimization messaging does not support route '{queueName}'.");
    }

    public IDisposable Subscribe<T>(string queueName, Func<T, Task> onMessage) {
        if (queueName != MessageRoutes.Request || typeof(T) != typeof(OptimizeRouteRequest)) {
            throw new NotSupportedException(
                "Azure optimization messaging only supports subscribing to optimization requests.");
        }

        var cts = new CancellationTokenSource();
        var task = Task.Run(
            () => ProcessRequestsAsync(
                async request => await onMessage((T)(object)request),
                cts.Token),
            CancellationToken.None);

        return new Subscription(cts, task);
    }

    private Task PublishRequestAsync<T>(T message) {
        var job = message switch {
            OptimizationJobMessage jobMessage => jobMessage,
            OptimizeRouteRequest request => new OptimizationJobMessage(
                request.TenantId,
                request.OptimizationRunId),
            _ => throw new NotSupportedException(
                $"Azure optimization request publishing does not support message type '{typeof(T).FullName}'.")
        };

        return jobQueue.EnqueueAsync(job, CancellationToken.None);
    }

    private Task PublishResponseAsync<T>(T message) {
        if (message is not OptimizeRouteResponse response) {
            throw new NotSupportedException(
                $"Azure optimization response publishing does not support message type '{typeof(T).FullName}'.");
        }

        return UploadResultAsync(response, CancellationToken.None);
    }

    private async Task ProcessRequestsAsync(
        Func<OptimizeRouteRequest, Task> onMessage,
        CancellationToken ct) {
        logger.LogInformation("Azure optimization message bus listening for Service Bus optimization jobs.");

        while (!ct.IsCancellationRequested) {
            try {
                var envelope = await jobQueue.ReceiveOneAsync(ct);
                if (envelope is null) {
                    continue;
                }

                await ProcessEnvelopeAsync(envelope, onMessage, ct);
            } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Error receiving Azure optimization job; retrying.");
                await DelayAfterReceiveFailureAsync(ct);
            }
        }
    }

    private async Task ProcessEnvelopeAsync(
        IOptimizationJobEnvelope envelope,
        Func<OptimizeRouteRequest, Task> onMessage,
        CancellationToken ct) {
        if (envelope.DeserializationException is not null || envelope.Message is null) {
            await envelope.DeadLetterAsync(
                "InvalidOptimizationJobMessage",
                envelope.DeserializationException?.Message ?? "Optimization job message body was empty.",
                ct);
            return;
        }

        var job = envelope.Message;
        var run = await runStore.GetAsync(job.TenantId, job.OptimizationRunId, ct);
        if (run is null) {
            var maxMissingRunDeliveries = GetMaxMissingRunDeliveries();
            if (envelope.DeliveryCount >= maxMissingRunDeliveries) {
                await envelope.DeadLetterAsync(
                    "OptimizationRunNotFound",
                    $"Optimization run {job.OptimizationRunId} for tenant {job.TenantId} was not found in Cosmos.",
                    ct);
            } else {
                await envelope.AbandonAsync(ct);
            }

            return;
        }

        try {
            await onMessage(run.RequestSnapshot);

            // The worker handler publishes the solver response through this bus. In Azure mode
            // that publish uploads to the API, so completion only happens after the API succeeds.
            await envelope.CompleteAsync(ct);
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "Optimization job {RunId} for tenant {TenantId} failed before the result was persisted.",
                job.OptimizationRunId,
                job.TenantId);
            await envelope.AbandonAsync(ct);
        }
    }

    private async Task UploadResultAsync(
        OptimizeRouteResponse response,
        CancellationToken ct) {
        var baseUrl = GetPlannerApiBaseUrl();
        var requestUri = new Uri(
            new Uri($"{baseUrl.TrimEnd('/')}/"),
            $"api/vrp/runs/{response.OptimizationRunId}/result");

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) {
            Content = JsonContent.Create(response, options: JsonOptions)
        };

        var workerResultApiKey = configuration["Optimization:WorkerResultApiKey"];
        if (!string.IsNullOrWhiteSpace(workerResultApiKey)) {
            request.Headers.Add(WorkerResultApiKeyHeader, workerResultApiKey);
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var httpResponse = await client.SendAsync(request, ct);
        if (httpResponse.IsSuccessStatusCode) {
            logger.LogInformation(
                "Uploaded optimization result for tenant {TenantId}, run {RunId}.",
                response.TenantId,
                response.OptimizationRunId);
            return;
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"Planner API rejected optimization result for run {response.OptimizationRunId}. "
            + $"Status {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}. {responseBody}");
    }

    private string GetPlannerApiBaseUrl() {
        var baseUrl = FirstNonWhiteSpace(
            configuration["PlannerApi:BaseUrl"],
            configuration["SignalR:Server"]);

        if (baseUrl is null) {
            throw new InvalidOperationException(
                "PlannerApi:BaseUrl is required for Azure optimization worker result uploads.");
        }

        return baseUrl;
    }

    private int GetMaxMissingRunDeliveries() =>
        int.TryParse(configuration["Optimization:MaxMissingRunDeliveries"], out var value) && value > 0
            ? value
            : 3;

    private static async Task DelayAfterReceiveFailureAsync(CancellationToken ct) {
        try {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            // normal shutdown
        }
    }

    private static string? FirstNonWhiteSpace(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private sealed class Subscription(
        CancellationTokenSource cancellationTokenSource,
        Task processingTask) : IDisposable {

        public void Dispose() {
            cancellationTokenSource.Cancel();

            try {
                processingTask.GetAwaiter().GetResult();
            } catch (OperationCanceledException) {
                // normal shutdown
            } finally {
                cancellationTokenSource.Dispose();
            }
        }
    }
}
