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

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddOptimizationRunInfrastructure();
builder.Services.AddOptimization();
builder.Services.AddSingleton<OptimizationJobProcessor>();

using var host = builder.Build();
var processor = host.Services.GetRequiredService<OptimizationJobProcessor>();
var exitCode = await processor.ProcessOneAsync(CancellationToken.None);
Environment.ExitCode = exitCode;
