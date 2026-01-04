using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Planner.API;
using Planner.API.BackgroundServices;
using Planner.API.Middleware;
using Planner.API.SignalR;
using Planner.Application;
using Planner.Infrastructure;
using Planner.Infrastructure.Auth;
using Planner.Infrastructure.Coordinator;
using Planner.Messaging.DependencyInjection;

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
// Auth
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Messaging & SignalR
builder.Services.AddMessagingBus();
builder.Services.AddRealtime(builder.Configuration);

// Background consumers / coordinators
builder.Services.AddHostedService<CoordinatorService>();
builder.Services.AddHostedService<OptimizeRouteResultConsumer>();

// Health checks
builder.Services.AddHealthChecks();

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

if (!app.Environment.IsProduction()) {
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantContextMiddleware>();

// Messaging & SignalR
app.UseRealtime();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

// CRUD endpoints are exposed via attribute-routed controllers under `Controllers/`.

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
        "SignalR:Route",
        "JwtOptions:Issuer",
        "JwtOptions:Audience",
        "JwtOptions:SigningKey",
        "JwtOptions:Secret"
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
