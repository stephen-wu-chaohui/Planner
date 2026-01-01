using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Planner.API;
using Planner.API.BackgroundServices;
using Planner.API.SignalR;
using Planner.Application;
using Planner.Infrastructure;
using Planner.Infrastructure.Coordinator;
using Planner.Messaging.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

//
// ────────────────────────────────────────────────
// Configuration
// ────────────────────────────────────────────────
//
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

//
// ────────────────────────────────────────────────
// Configuration validation (fail fast)
// ────────────────────────────────────────────────
//
ValidateRequiredConfiguration(builder.Configuration);

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

// Messaging & SignalR
builder.Services.AddMessagingBus();
builder.Services.AddRealtime(builder.Configuration);

// Background consumers / coordinators
builder.Services.AddHostedService<CoordinatorService>();
builder.Services.AddHostedService<OptimizeRouteResultConsumer>();

// Health checks
builder.Services.AddHealthChecks();

// Tenant context (placeholder, intentionally simple)
builder.Services.AddScoped<ITenantContext, StaticTenantContext>();

//
// ────────────────────────────────────────────────
// Build app
// ────────────────────────────────────────────────
//
var app = builder.Build();

// ────────────────────────────────────────────────
// Forwarded headers (for working behind reverse proxies)
// ────────────────────────────────────────────────

var forwardedHeadersOptions = new ForwardedHeadersOptions {
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);


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

// Messaging & SignalR
app.UseRealtime();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

app.MapApiEndpoints();

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
        "RabbitMq:Pass",
        "SignalR:Client",
        "SignalR:Route"
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
