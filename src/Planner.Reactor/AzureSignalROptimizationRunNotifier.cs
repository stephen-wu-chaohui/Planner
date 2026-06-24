using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Planner.Contracts.OptimizationRuns;

namespace Planner.Reactor;

public sealed class AzureSignalROptimizationRunNotifier(
    IConfiguration configuration,
    ILogger<AzureSignalROptimizationRunNotifier> logger) : IOptimizationRunNotifier, IAsyncDisposable {
    private const string DefaultHubName = "planner";
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IServiceHubContext? _hubContext;

    public async Task SendRunChangedAsync(OptimizationRunChangedDto notification, CancellationToken ct) {
        var hub = await GetHubContextAsync(ct);
        if (hub is null) {
            return;
        }

        await hub.Clients
            .User(notification.TenantId.ToString())
            .SendCoreAsync("optimizationRunChanged", [notification], ct);
    }

    public async Task SendInsightChangedAsync(OptimizationInsightChangedDto notification, CancellationToken ct) {
        var hub = await GetHubContextAsync(ct);
        if (hub is null) {
            return;
        }

        await hub.Clients
            .User(notification.TenantId.ToString())
            .SendCoreAsync("optimizationInsightChanged", [notification], ct);
    }

    public async ValueTask DisposeAsync() {
        if (_hubContext is not null) {
            await _hubContext.DisposeAsync();
        }
    }

    private async Task<IServiceHubContext?> GetHubContextAsync(CancellationToken ct) {
        if (_hubContext is not null) {
            return _hubContext;
        }

        await _initLock.WaitAsync(ct);
        try {
            if (_hubContext is not null) {
                return _hubContext;
            }

            var connectionString = configuration["SignalR:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString)) {
                logger.LogWarning("SignalR:ConnectionString is not configured. Reactor notifications are disabled.");
                return null;
            }

            var hubName = configuration["SignalR:HubName"] ?? DefaultHubName;
            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(options => options.ConnectionString = connectionString)
                .BuildServiceManager();

            _hubContext = await serviceManager.CreateHubContextAsync(hubName, ct);
            return _hubContext;
        } finally {
            _initLock.Release();
        }
    }
}
