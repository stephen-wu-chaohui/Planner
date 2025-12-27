using Planner.Messaging.DependencyInjection;
using Planner.Optimization.DependencyInjection;
using Planner.Optimization.Worker.BackgroundServices;
using Planner.Optimization.Worker.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Try to load shared.appsettings.json only if it exists
var sharedConfigPath = Path.Combine(AppContext.BaseDirectory, "shared.appsettings.json");
var loggerFactory = LoggerFactory.Create(config => {
    config.AddConsole();
});
var logger = loggerFactory.CreateLogger("Startup");

if (File.Exists(sharedConfigPath)) {
    builder.Configuration.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);
    logger.LogInformation("Loaded shared.appsettings.json from {Path}", sharedConfigPath);
} else {
    logger.LogWarning("shared.appsettings.json not found — continuing with appsettings.json & environment variables.");
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// =========================================================
// SERVICES
// =========================================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Shared infrastructure
builder.Services.AddMessagingBus();

// --- Optimization ---
builder.Services.AddOptimization();

// Your worker services
builder.Services.AddHostedService<OptimizationWorker>();
builder.Services.AddTransient<IOptimizationRequestHandler, OptimizationRequestHandler>();

var host = builder.Build();
await host.RunAsync();
