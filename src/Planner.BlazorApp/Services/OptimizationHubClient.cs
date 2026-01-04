using Microsoft.AspNetCore.SignalR.Client;
using Planner.Contracts.Optimization.Responses;
using Planner.Messaging;

namespace Planner.BlazorApp.Services;

public sealed class OptimizationHubClient(IConfiguration configuration, ILogger<OptimizationHubClient> logger) : IOptimizationHubClient {
    private HubConnection? _connection;

    public event Action<OptimizeRouteResponse>? OptimizationCompleted;

    public async Task ConnectAsync(Guid tenantId, Guid? optimizationRunId = null) {
        if (_connection != null)
        {
            logger.LogDebug("SignalR connection already exists, skipping reconnection");
            return;
        }

        try {
            // Get configuration values
            var signalRServer = configuration["SignalR:Server"] ?? configuration["SignalR__Server"];
            var signalRRoute = configuration["SignalR:Route"] ?? configuration["SignalR__Route"];

            if (string.IsNullOrEmpty(signalRServer))
            {
                logger.LogWarning("SignalR:Server configuration not found, skipping SignalR connection");
                return;
            }

            if (string.IsNullOrEmpty(signalRRoute))
            {
                signalRRoute = "/hubs/planner"; // Default route
                logger.LogInformation("Using default SignalR route: {Route}", signalRRoute);
            }

            var hubUrl = $"{signalRServer.TrimEnd('/')}{signalRRoute}";
            logger.LogInformation("Connecting to SignalR hub at: {HubUrl}", hubUrl);

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // For development, ignore SSL certificate errors
                    if (configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development")
                    {
                        options.HttpMessageHandlerFactory = _ => new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                        };
                    }
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10) })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Register event handlers
            _connection.On<OptimizeRouteResponse>(
                MessageRoutes.Response,
                evt => 
                {
                    logger.LogInformation("Received OptimizationCompleted event for tenant {TenantId}", evt.TenantId);
                    OptimizationCompleted?.Invoke(evt);
                });

            _connection.Closed += async (error) =>
            {
                if (error != null)
                    logger.LogError(error, "SignalR connection closed with error");
                else
                    logger.LogInformation("SignalR connection closed normally");
            };

            _connection.Reconnecting += (error) =>
            {
                logger.LogWarning(error, "SignalR connection lost, attempting to reconnect...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            logger.LogInformation("SignalR connection established. ConnectionId: {ConnectionId}", _connection.ConnectionId);

            // Join tenant group
            await _connection.InvokeAsync("JoinTenant", tenantId);
            logger.LogInformation("Joined tenant group for {TenantId}", tenantId);

            // Optionally join optimization run group
            if (optimizationRunId.HasValue) {
                await _connection.InvokeAsync("JoinOptimizationRun", tenantId, optimizationRunId.Value);
                logger.LogInformation("Joined optimization run group for {TenantId}:{OptimizationRunId}", tenantId, optimizationRunId.Value);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to SignalR hub. This is not critical - the application will continue without real-time updates.");
            
            // Don't throw - allow the app to continue without SignalR
            await DisconnectAsync();
        }
    }

    public async Task DisconnectAsync() {
        if (_connection == null)
            return;

        try 
        {
            logger.LogInformation("Disconnecting from SignalR hub");
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            logger.LogInformation("SignalR disconnection completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during SignalR disconnection");
        }
        finally 
        {
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync() {
        await DisconnectAsync();
    }
}
