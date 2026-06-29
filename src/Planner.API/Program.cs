using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Planner.API.BackgroundServices;
using Planner.API.GraphQL;
using Planner.API.Hubs;
using Planner.API.Middleware;
using Planner.API.Services;
using Planner.Application;
using Planner.Application.OptimizationRuns;
using Planner.Infrastructure;
using Planner.Infrastructure.Coordinator;
using Planner.Messaging;

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────
// Configuration
// ────────────────────────────────────────────────
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

ValidateRequiredConfiguration(builder.Configuration);
var useAzureOptimizationDispatch = UseAzureServiceBusDispatch(builder.Configuration);
const string BlazorClientCorsPolicy = "BlazorClient";

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
builder.Services.AddCors(options =>
{
    options.AddPolicy(BlazorClientCorsPolicy, policy =>
    {
        var origins = GetAllowedCorsOrigins(builder.Configuration);
        if (origins.Length > 0)
        {
            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// API Services
builder.Services.AddApplication();
builder.Services.AddScoped<IMatrixCalculationService, MatrixCalculationService>();
builder.Services.AddScoped<IOptimizationRunSnapshotBuilder, OptimizationRunSnapshotBuilder>();
builder.Services.AddScoped<IAzureSignalRConnectionInfoService, AzureSignalRConnectionInfoService>();
builder.Services.AddScoped<IOptimizationRunNotificationPublisher, SignalROptimizationRunNotificationPublisher>();

var signalRBuilder = builder.Services.AddSignalR();
if (UseAzureSignalRService(builder.Configuration, builder.Environment)) {
    signalRBuilder.AddAzureSignalR(options => {
        options.ConnectionString = builder.Configuration["SignalR:ConnectionString"];
    });
}

// Application / Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();

// --- UPDATED AUTHENTICATION SECTION ---
// We replace builder.Services.AddJwtAuthentication(...) with Microsoft Identity Web
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => {
    var originalOnMessageReceived = options.Events.OnMessageReceived;
    options.Events.OnMessageReceived = async context => {
        if (originalOnMessageReceived is not null) {
            await originalOnMessageReceived(context);
        }

        if (!string.IsNullOrWhiteSpace(context.Token)) {
            return;
        }

        if (context.Request.Path.StartsWithSegments(PlannerHub.Route)
            && context.Request.Query.TryGetValue("access_token", out var accessToken)) {
            context.Token = accessToken;
        }
    };
});

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
    builder.Services.AddAzureOptimizationMessaging();
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
app.UseCors(BlazorClientCorsPolicy);

// Order is important: Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom tenant middleware (Ensure this is refactored to use Claims)
app.UseMiddleware<TenantContextMiddleware>();

app.MapControllers();
app.MapHub<PlannerHub>(PlannerHub.Route).RequireAuthorization();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapGraphQL("/graphql").RequireAuthorization();

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

    var dispatchMode = OptimizationDispatchMode(config);
    if (UseAzureServiceBusDispatch(config)) {
        requiredKeys.Add("ServiceBus:ConnectionString");
        requiredKeys.Add("Optimization:WorkerResultApiKey");

        var hasCosmosConnectionString = !string.IsNullOrWhiteSpace(config["Cosmos:ConnectionString"]);
        var hasCosmosEndpointAndKey =
            !string.IsNullOrWhiteSpace(config["Cosmos:Endpoint"]) &&
            !string.IsNullOrWhiteSpace(config["Cosmos:Key"]);

        if (!hasCosmosConnectionString && !hasCosmosEndpointAndKey) {
            requiredKeys.Add("Cosmos:ConnectionString or Cosmos:Endpoint plus Cosmos:Key");
        }
    } else if (IsRabbitMqDispatch(dispatchMode)) {
        requiredKeys.Add("RabbitMq:Host");
    } else {
        throw new InvalidOperationException(
            $"Unsupported optimization dispatch mode '{dispatchMode}'. Supported values are RabbitMq, AzureServiceBus, and OptimizationMessaging:Transport values RabbitMQ or ServiceBus.");
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
        StringComparison.OrdinalIgnoreCase)
    || string.Equals(
        config["OptimizationMessaging:Transport"],
        "ServiceBus",
        StringComparison.OrdinalIgnoreCase);

static string OptimizationDispatchMode(IConfiguration config) =>
    config["Optimization:DispatchMode"]
    ?? config["OptimizationMessaging:Transport"]
    ?? "RabbitMq";

static bool IsRabbitMqDispatch(string dispatchMode) =>
    string.Equals(dispatchMode, "RabbitMq", StringComparison.OrdinalIgnoreCase)
    || string.Equals(dispatchMode, "RabbitMQ", StringComparison.OrdinalIgnoreCase);

static bool UseAzureSignalRService(IConfiguration config, IHostEnvironment environment) =>
    environment.IsProduction() &&
    !string.IsNullOrWhiteSpace(config["SignalR:ConnectionString"]);

static string[] GetAllowedCorsOrigins(IConfiguration config) {
    var configuredOrigins = config
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    var validOrigins = configuredOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (validOrigins.Length > 0) {
        return validOrigins;
    }

    return [
        "https://localhost:7014",
        "http://localhost:5212"
    ];
}

public partial class Program { }
