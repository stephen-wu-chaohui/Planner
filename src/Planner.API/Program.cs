using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Planner.API.BackgroundServices;
using Planner.Application;
using Planner.Infrastructure;
using Planner.Infrastructure.Coordinator;
using Planner.Messaging.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

//
// ────────────────────────────────────────────────
// Configuration
// ────────────────────────────────────────────────
//
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

//
// ────────────────────────────────────────────────
// Logging
// ────────────────────────────────────────────────
//
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//
// ────────────────────────────────────────────────
// Service registration
// ────────────────────────────────────────────────
//

// Controllers (API only)
builder.Services.AddControllers();

// Application / Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Messaging
builder.Services.AddMessagingBus();

// Background consumers / coordinators
builder.Services.AddHostedService<CoordinatorService>();
builder.Services.AddHostedService<OptimizeRouteResultConsumer>();

// Health checks
builder.Services.AddHealthChecks();

// Tenant context (placeholder, intentionally simple)
builder.Services.AddScoped<ITenantContext, StaticTenantContext>();

//
// ────────────────────────────────────────────────
// Configuration validation (fail fast)
// ────────────────────────────────────────────────
//
ValidateRequiredConfiguration(builder.Configuration);

//
// ────────────────────────────────────────────────
// Build app
// ────────────────────────────────────────────────
//
var app = builder.Build();

//
// ────────────────────────────────────────────────
// Middleware pipeline
// ────────────────────────────────────────────────
//
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();


//
// ────────────────────────────────────────────────
// Local helpers
// ────────────────────────────────────────────────
//
static void ValidateRequiredConfiguration(IConfiguration config)
{
    var requiredKeys = new[]
    {
        "ConnectionStrings:PlannerDb",
        "RabbitMq:Host",
        "RabbitMq:Port",
        "RabbitMq:User",
        "RabbitMq:Pass"
    };

    var missing = requiredKeys
        .Where(k => string.IsNullOrWhiteSpace(config[k]))
        .ToList();

    if (missing.Any())
    {
        throw new InvalidOperationException(
            $"Missing required configuration values: {string.Join(", ", missing)}"
        );
    }
}
