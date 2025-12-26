using Planner.Messaging.DependencyInjection;
using Planner.Optimization.DependencyInjection;
using Planner.Optimization.Worker.BackgroundServices;
using Planner.Optimization.Worker.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
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

// =========================================================
// BUILD APP (bind Kestrel) — required on Windows App Service
// =========================================================

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Run();
