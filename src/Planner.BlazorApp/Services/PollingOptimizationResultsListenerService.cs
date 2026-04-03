using Planner.Contracts.Optimization;

namespace Planner.BlazorApp.Services;

/// <summary>
/// Blazor WebAssembly-compatible implementation of <see cref="IOptimizationResultsListenerService"/>.
/// Polls the API for optimization results instead of using server-side Firestore listeners.
/// </summary>
public sealed class PollingOptimizationResultsListenerService(
    PlannerApiClient api,
    ILogger<PollingOptimizationResultsListenerService> logger) : IOptimizationResultsListenerService
{
    private const int PollingIntervalMs = 3000;
    private const int MaxPollAttempts = 60;

    private readonly HashSet<Guid> _pendingRunIds = [];
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private CancellationTokenSource? _cts;

    public event Action<RoutingResultDto>? OnOptimizationCompleted;

    public Task StartListeningAsync(Guid tenantId)
    {
        logger.LogInformation("Polling optimization results listener started for tenant {TenantId}", tenantId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// Enqueues the run ID for polling and starts background polling if not already running.
    public Task NotifyOptimizationRunStartedAsync(Guid runId)
    {
        logger.LogInformation("Optimization run started: {RunId}. Beginning poll.", runId);

        lock (_pendingRunIds)
        {
            _pendingRunIds.Add(runId);
        }

        StartPollingIfNotRunning();
        return Task.CompletedTask;
    }

    public Task StopListeningAsync()
    {
        CancelPolling();
        logger.LogInformation("Polling optimization results listener stopped");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        CancelPolling();
        _pollLock.Dispose();
        await Task.CompletedTask;
    }

    private void StartPollingIfNotRunning()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
            return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await PollAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in optimization result polling loop");
            }
        }, token);
    }

    private void CancelPolling()
    {
        _cts?.Cancel();
        _cts = null;
        lock (_pendingRunIds)
        {
            _pendingRunIds.Clear();
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        var attemptsPerRun = new Dictionary<Guid, int>();

        while (!cancellationToken.IsCancellationRequested)
        {
            Guid[] currentRuns;
            lock (_pendingRunIds)
            {
                currentRuns = [.. _pendingRunIds];
            }

            if (currentRuns.Length == 0)
                break;

            await _pollLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var completedRuns = new List<Guid>();

                foreach (var runId in currentRuns)
                {
                    if (!attemptsPerRun.TryGetValue(runId, out var attempts))
                        attempts = 0;

                    var result = await FetchResultAsync(runId, cancellationToken).ConfigureAwait(false);

                    if (result != null)
                    {
                        completedRuns.Add(runId);
                        attemptsPerRun.Remove(runId);
                        logger.LogInformation("Optimization result received via polling: Run {RunId}", runId);
                        OnOptimizationCompleted?.Invoke(result);
                    }
                    else
                    {
                        attempts++;
                        attemptsPerRun[runId] = attempts;
                        logger.LogDebug("Optimization result not yet available for run {RunId} (attempt {Attempt})", runId, attempts);

                        if (attempts >= MaxPollAttempts)
                        {
                            completedRuns.Add(runId);
                            attemptsPerRun.Remove(runId);
                            logger.LogWarning("Max poll attempts reached for run {RunId}. Result may not have been delivered.", runId);
                        }
                    }
                }

                lock (_pendingRunIds)
                {
                    foreach (var id in completedRuns)
                        _pendingRunIds.Remove(id);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error polling for optimization results");
            }
            finally
            {
                _pollLock.Release();
            }

            await Task.Delay(PollingIntervalMs, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<RoutingResultDto?> FetchResultAsync(Guid runId, CancellationToken cancellationToken)
    {
        try
        {
            return await api.GetFromJsonAsync<RoutingResultDto>($"api/vrp/results/{runId}", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Polling request failed for run {RunId}", runId);
            return null;
        }
    }
}
