using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using Planner.Optimization;

namespace Planner.Optimization.JobWorker;

public sealed class OptimizationJobProcessor(
    IOptimizationJobQueue queue,
    IOptimizationRunStore runStore,
    IRouteOptimizer optimizer,
    IConfiguration configuration,
    ILogger<OptimizationJobProcessor> logger) {

    public async Task<int> ProcessOneAsync(CancellationToken ct) {
        var envelope = await queue.ReceiveOneAsync(ct);
        if (envelope is null) {
            logger.LogInformation("No optimization job message available.");
            return 0;
        }

        if (envelope.DeserializationException is not null || envelope.Message is null) {
            await envelope.DeadLetterAsync(
                "InvalidOptimizationJobMessage",
                envelope.DeserializationException?.Message ?? "Message body could not be read.",
                ct);
            return 0;
        }

        var message = envelope.Message;
        try {
            var run = await runStore.GetAsync(message.TenantId, message.OptimizationRunId, ct);
            if (run is null) {
                await envelope.DeadLetterAsync(
                    "OptimizationRunNotFound",
                    $"Optimization run {message.OptimizationRunId} was not found for tenant {message.TenantId}.",
                    ct);
                return 0;
            }

            if (IsTerminal(run.Status)) {
                logger.LogInformation(
                    "Optimization run {RunId} is already terminal with status {Status}; completing duplicate message.",
                    run.OptimizationRunId,
                    run.Status);
                await envelope.CompleteAsync(ct);
                return 0;
            }

            var attempt = new OptimizationRunAttemptDto(
                AttemptId: Guid.NewGuid().ToString("N"),
                StartedAtUtc: DateTime.UtcNow,
                CompletedAtUtc: null,
                WorkerId: GetWorkerId(),
                DeliveryCount: envelope.DeliveryCount,
                Status: OptimizationRunStatus.Running,
                ErrorMessage: null);

            var started = await runStore.TryStartAttemptAsync(
                message.TenantId,
                message.OptimizationRunId,
                attempt,
                ct);

            if (!started) {
                await envelope.CompleteAsync(ct);
                return 0;
            }

            try {
                var result = optimizer.Optimize(run.RequestSnapshot);
                await runStore.SaveSolverResultAsync(message.TenantId, message.OptimizationRunId, result, ct);
                await envelope.CompleteAsync(ct);
                return 0;
            } catch (Exception ex) {
                logger.LogError(ex, "Solver failed for optimization run {RunId}.", message.OptimizationRunId);
                await runStore.SaveFailureAsync(
                    message.TenantId,
                    message.OptimizationRunId,
                    ex.Message,
                    OptimizationRunStatus.Failed,
                    ct);
                await envelope.CompleteAsync(ct);
                return 0;
            }
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "Transient processing failure for optimization job message {MessageId}; abandoning message.",
                envelope.MessageId);
            await envelope.AbandonAsync(CancellationToken.None);
            return 1;
        }
    }

    private string GetWorkerId() =>
        configuration["Worker:Id"]
        ?? Environment.GetEnvironmentVariable("HOSTNAME")
        ?? Environment.MachineName;

    private static bool IsTerminal(OptimizationRunStatus status) =>
        status is OptimizationRunStatus.Succeeded
            or OptimizationRunStatus.Failed
            or OptimizationRunStatus.DeadLettered
            or OptimizationRunStatus.Cancelled;
}
