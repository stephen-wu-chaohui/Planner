using Microsoft.AspNetCore.SignalR.Client;
using Planner.BlazorApp.Auth;
using Planner.Contracts.Optimization;

namespace Planner.BlazorApp.Services;

public sealed class OptimizationHubClient(
    IConfiguration configuration,
    ILogger<OptimizationHubClient> logger,
    IJwtTokenStore tokenStore) : IOptimizationHubClient {
    private HubConnection? _connection;

    public event Action<RoutingResultDto>? OptimizationCompleted;

    public async Task ConnectAsync(Guid? optimizationRunId = null) {
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
                    options.AccessTokenProvider = () => Task.FromResult(tokenStore.AccessToken)!;

                    // For development, ignore SSL certificate errors
                    if (configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development")
                    {
                        options.HttpMessageHandlerFactory = _ => new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                        };
                    }
                })
                .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)])
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Register event handlers
            _connection.On<RoutingResultDto>(
                "RoutingResultDto",
                evt => 
                {
                    logger.LogInformation("Received OptimizationCompleted event for run {OptimizationRunId}", evt.OptimizationRunId);
                    OptimizationCompleted?.Invoke(evt);
                });

            _connection.Closed += (error) =>
            {
                if (error != null)
                    logger.LogError(error, "SignalR connection closed with error");
                else
                    logger.LogInformation("SignalR connection closed normally");
                return Task.CompletedTask;
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

            // Optionally join optimization run group
            if (optimizationRunId.HasValue) {
                await _connection.InvokeAsync("JoinOptimizationRun", optimizationRunId.Value);
                logger.LogInformation("Joined optimization run group for {OptimizationRunId}", optimizationRunId.Value);
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
