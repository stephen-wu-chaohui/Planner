using Microsoft.AspNetCore.SignalR.Client;
using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;

namespace Planner.BlazorApp.Services;

public sealed class SignalROptimizationResultsListenerService(
    PlannerApiClient api,
    ILogger<SignalROptimizationResultsListenerService> logger) : IOptimizationResultsListenerService {
    private HubConnection? _connection;
    private Guid _tenantId;

    public event Action<OptimizationRunChangedDto>? OnOptimizationRunChanged;
    public event Action<RoutingResultDto>? OnOptimizationCompleted;

    public async Task StartListeningAsync(Guid tenantId) {
        if (_connection is not null) {
            logger.LogDebug("SignalR optimization listener already running");
            return;
        }

        _tenantId = tenantId;
        var connectionInfo = await api.GetFromJsonAsync<SignalRConnectionInfoDto>("/api/realtime/negotiate");
        if (connectionInfo is null) {
            logger.LogWarning("SignalR negotiate returned no connection info.");
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(connectionInfo.Url, options => {
                options.AccessTokenProvider = () => Task.FromResult<string?>(connectionInfo.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<OptimizationRunChangedDto>(
            "optimizationRunChanged",
            async notification => await HandleRunChangedAsync(notification));

        await _connection.StartAsync();
        logger.LogInformation("SignalR optimization listener started.");
    }

    public async Task StopListeningAsync() {
        if (_connection is null) {
            return;
        }

        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
        logger.LogInformation("SignalR optimization listener stopped.");
    }

    public async ValueTask DisposeAsync() {
        await StopListeningAsync();
    }

    private async Task HandleRunChangedAsync(OptimizationRunChangedDto notification) {
        if (notification.TenantId != _tenantId) {
            return;
        }

        OnOptimizationRunChanged?.Invoke(notification);

        if (!notification.HasResult) {
            return;
        }

        try {
            var result = await api.GetFromJsonAsync<RoutingResultDto>(
                $"/api/vrp/runs/{notification.OptimizationRunId}/result");
            if (result is not null) {
                OnOptimizationCompleted?.Invoke(result);
            }
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "Failed to fetch optimization result for run {RunId}.",
                notification.OptimizationRunId);
        }
    }
}
