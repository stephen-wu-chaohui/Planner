using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;

namespace Planner.BlazorApp.Services;

public sealed class SignalROptimizationResultsListenerService(
    PlannerApiClient api,
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration,
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

        var connection = new HubConnectionBuilder()
            .WithUrl(BuildHubUrl(), options => {
                options.AccessTokenProvider = GetAccessTokenAsync;
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<OptimizationRunChangedDto>(
            "optimizationRunChanged",
            async notification => await HandleRunChangedAsync(notification));
        connection.On<RoutingResultDto>(
            "optimizationCompleted",
            HandleOptimizationCompleted);

        try {
            await connection.StartAsync();
        } catch (Exception ex) {
            await connection.DisposeAsync();
            logger.LogWarning(ex, "SignalR connection failed. Realtime optimization notifications are disabled.");
            return;
        }

        _connection = connection;
        logger.LogInformation("SignalR optimization listener started.");
    }

    private string BuildHubUrl() {
        var apiBaseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl not configured");
        var route = configuration["SignalR:Route"] ?? "/hubs/planner";

        return new Uri(
            new Uri($"{apiBaseUrl.TrimEnd('/')}/"),
            route.TrimStart('/')).ToString();
    }

    private async Task<string?> GetAccessTokenAsync() {
        var apiScope = configuration["Api:Scope"];
        if (string.IsNullOrWhiteSpace(apiScope)) {
            logger.LogWarning("Api:Scope is not configured. SignalR access token cannot be requested.");
            return null;
        }

        var tokenResult = await accessTokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions { Scopes = [apiScope] });
        return tokenResult.TryGetToken(out var token) ? token.Value : null;
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

    private void HandleOptimizationCompleted(RoutingResultDto result) {
        if (result.TenantId != _tenantId) {
            return;
        }

        OnOptimizationCompleted?.Invoke(result);
    }
}
