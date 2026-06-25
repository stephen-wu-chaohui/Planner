using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Planner.API.BackgroundServices;
using Planner.API.GraphQL;
using Planner.API.Middleware;
using Planner.API.Services;
using Planner.Application;
using Planner.Application.OptimizationRuns;
using Planner.Infrastructure;
using Planner.Infrastructure.Coordinator;
using Planner.Messaging;
using Planner.Messaging.Messaging;

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────
// Configuration
// ────────────────────────────────────────────────
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

ValidateRequiredConfiguration(builder.Configuration);
var useAzureOptimizationDispatch = UseAzureServiceBusDispatch(builder.Configuration);

// ────────────────────────────────────────────────
// Logging
// ────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();


// ────────────────────────────────────────────────
// Service registration
// ────────────────────────────────────────────────

// Controllers (API only)
builder.Services.AddControllers();

// API Services
builder.Services.AddApplication();
builder.Services.AddScoped<IMatrixCalculationService, MatrixCalculationService>();
builder.Services.AddScoped<IOptimizationRunSnapshotBuilder, OptimizationRunSnapshotBuilder>();
builder.Services.AddScoped<IAzureSignalRConnectionInfoService, AzureSignalRConnectionInfoService>();

// Application / Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();

// --- UPDATED AUTHENTICATION SECTION ---
// We replace builder.Services.AddJwtAuthentication(...) with Microsoft Identity Web
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// Maintain your custom tenant context, now powered by Entra ID claims
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();


// --- UPDATED GRAPHQL SECTION ---
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .AddHttpRequestInterceptor<TenantContextInterceptor>();
// ---------------------------------------

// Messaging
if (useAzureOptimizationDispatch) {
    builder.Services.AddSingleton<IMessageBus, DisabledLegacyOptimizationMessageBus>();
} else {
    builder.Services.AddMessagingBus();
}
builder.Services.AddScoped<IRouteEnrichmentService, RouteEnrichmentService>();

// Background consumers / coordinators
builder.Services.AddHostedService<CoordinatorService>();
if (!useAzureOptimizationDispatch) {
    builder.Services.AddHostedService<OptimizeRouteResultConsumer>();
}

// Health checks
builder.Services.AddHealthChecks();

// ────────────────────────────────────────────────
// Build app
// ────────────────────────────────────────────────
var app = builder.Build();

// Forwarded headers (for working behind reverse proxies)
var forwardedHeadersOptions = new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// ────────────────────────────────────────────────
// Middleware pipeline
// ────────────────────────────────────────────────
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
} else {
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

if (!app.Environment.IsProduction()) {
    app.UseHttpsRedirection();
}

app.UseRouting();

// Order is important: Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom tenant middleware (Ensure this is refactored to use Claims)
app.UseMiddleware<TenantContextMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapGraphQL("/graphql");

app.Run();

// ────────────────────────────────────────────────
// Local helpers
// ────────────────────────────────────────────────
static void ValidateRequiredConfiguration(IConfiguration config) {
    var requiredKeys = new List<string>
    {
        "ConnectionStrings:PlannerDb",
        "AzureAd:Instance",
        "AzureAd:TenantId",
        "AzureAd:ClientId"
    };

    var dispatchMode = config["Optimization:DispatchMode"] ?? "RabbitMq";
    if (UseAzureServiceBusDispatch(config)) {
        requiredKeys.Add("ServiceBus:ConnectionString");

        var hasCosmosConnectionString = !string.IsNullOrWhiteSpace(config["Cosmos:ConnectionString"]);
        var hasCosmosEndpointAndKey =
            !string.IsNullOrWhiteSpace(config["Cosmos:Endpoint"]) &&
            !string.IsNullOrWhiteSpace(config["Cosmos:Key"]);

        if (!hasCosmosConnectionString && !hasCosmosEndpointAndKey) {
            requiredKeys.Add("Cosmos:ConnectionString or Cosmos:Endpoint plus Cosmos:Key");
        }
    } else if (string.Equals(dispatchMode, "RabbitMq", StringComparison.OrdinalIgnoreCase)) {
        requiredKeys.Add("RabbitMq:Host");
    } else {
        throw new InvalidOperationException(
            $"Unsupported Optimization:DispatchMode '{dispatchMode}'. Supported values are RabbitMq and AzureServiceBus.");
    }

    var missing = requiredKeys
        .Where(k => string.IsNullOrWhiteSpace(config[k]))
        .ToList();

    if (missing.Count != 0) {
        throw new InvalidOperationException(
            $"Missing required configuration values: {string.Join(", ", missing)}"
        );
    }
}

static bool UseAzureServiceBusDispatch(IConfiguration config) =>
    string.Equals(
        config["Optimization:DispatchMode"],
        "AzureServiceBus",
        StringComparison.OrdinalIgnoreCase);

public partial class Program { }

internal sealed class DisabledLegacyOptimizationMessageBus : IMessageBus {
    public Task PublishAsync<T>(string queueName, T message) =>
        throw new InvalidOperationException(
            "RabbitMQ optimization messaging is disabled because Optimization:DispatchMode is AzureServiceBus.");

    public IDisposable Subscribe<T>(string queueName, Func<T, Task> onMessage) =>
        throw new InvalidOperationException(
            "RabbitMQ optimization messaging is disabled because Optimization:DispatchMode is AzureServiceBus.");
}
