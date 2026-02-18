using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Planner.API.BackgroundServices;
using Planner.API.GraphQL;
using Planner.API.Middleware;
using Planner.API.Services;
using Planner.Application;
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
builder.Services.AddScoped<IMatrixCalculationService, MatrixCalculationService>();

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
builder.Services.AddMessagingBus();
builder.Services.AddScoped<IRouteEnrichmentService, RouteEnrichmentService>();

// Background consumers / coordinators
builder.Services.AddHostedService<CoordinatorService>();
builder.Services.AddHostedService<OptimizeRouteResultConsumer>();

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
forwardedHeadersOptions.KnownNetworks.Clear();
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
    // Updated to reflect the shift to Entra ID
    var requiredKeys = new[]
    {
        "ConnectionStrings:PlannerDb",
        "RabbitMq:Host",
        "AzureAd:Instance",
        "AzureAd:TenantId",
        "AzureAd:ClientId"
    };

    var missing = requiredKeys
        .Where(k => string.IsNullOrWhiteSpace(config[k]))
        .ToList();

    if (missing.Count != 0) {
        throw new InvalidOperationException(
            $"Missing required configuration values for Entra ID integration: {string.Join(", ", missing)}"
        );
    }
}

public partial class Program { }