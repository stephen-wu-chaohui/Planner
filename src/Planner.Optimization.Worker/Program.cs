using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planner.Infrastructure;
using Planner.Messaging;
using Planner.Optimization;
using Planner.Optimization.Worker.BackgroundServices;
using Planner.Optimization.Worker.Handlers;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var useAzureOptimizationDispatch = UseAzureOptimizationDispatch(builder.Configuration);

var loggerFactory = LoggerFactory.Create(config => {
    config.AddConsole();
});
var logger = loggerFactory.CreateLogger("Startup");

logger.LogInformation(
    "Optimization worker dispatch mode: {DispatchMode}",
    useAzureOptimizationDispatch ? "AzureServiceBus" : "RabbitMq");

// =========================================================
// SERVICES
// =========================================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Shared infrastructure
if (useAzureOptimizationDispatch) {
    builder.Services.AddAzureOptimizationMessaging();
} else {
    builder.Services.AddMessagingBus();
}

// --- Optimization ---
builder.Services.AddOptimization();

// Worker services
builder.Services.AddHostedService<OptimizationWorker>();
builder.Services.AddTransient<IOptimizationRequestHandler, OptimizationRequestHandler>();

var host = builder.Build();
await host.RunAsync();

static bool UseAzureOptimizationDispatch(IConfiguration configuration) =>
    string.Equals(
        configuration["Optimization:DispatchMode"],
        "AzureServiceBus",
        StringComparison.OrdinalIgnoreCase)
    || string.Equals(
        configuration["OptimizationMessaging:Transport"],
        "ServiceBus",
        StringComparison.OrdinalIgnoreCase);
