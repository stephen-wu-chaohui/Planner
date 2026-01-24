using Planner.Messaging.DependencyInjection;
using Planner.Optimization.Worker.BackgroundServices;
using Planner.Optimization.Worker.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Planner.Optimization;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddEnvironmentVariables();

// Try to load shared.appsettings.json only if it exists
var loggerFactory = LoggerFactory.Create(config => {
    config.AddConsole();
});
var logger = loggerFactory.CreateLogger("Startup");


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
