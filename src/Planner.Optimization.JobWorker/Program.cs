using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planner.Infrastructure;
using Planner.Optimization;
using Planner.Optimization.JobWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
ValidateRequiredConfiguration(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddOptimizationRunInfrastructure();
builder.Services.AddOptimization();
builder.Services.AddSingleton<OptimizationJobProcessor>();

using var host = builder.Build();
var processor = host.Services.GetRequiredService<OptimizationJobProcessor>();
var exitCode = await processor.ProcessOneAsync(CancellationToken.None);
Environment.ExitCode = exitCode;

static void ValidateRequiredConfiguration(IConfiguration config) {
    var requiredKeys = new List<string>
    {
        "ServiceBus:ConnectionString"
    };

    var hasCosmosConnectionString = !string.IsNullOrWhiteSpace(config["Cosmos:ConnectionString"]);
    var hasCosmosEndpointAndKey =
        !string.IsNullOrWhiteSpace(config["Cosmos:Endpoint"]) &&
        !string.IsNullOrWhiteSpace(config["Cosmos:Key"]);

    if (!hasCosmosConnectionString && !hasCosmosEndpointAndKey) {
        requiredKeys.Add("Cosmos:ConnectionString or Cosmos:Endpoint plus Cosmos:Key");
    }

    var missing = requiredKeys
        .Where(k => string.IsNullOrWhiteSpace(config[k]))
        .ToList();

    if (missing.Count != 0) {
        throw new InvalidOperationException(
            $"Missing required optimization job worker configuration values: {string.Join(", ", missing)}");
    }
}
