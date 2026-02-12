using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Planner.API.BackgroundServices;
using Planner.API.GraphQL;
using Planner.API.Middleware;
using Planner.Application;
using Planner.Infrastructure;
using Planner.Infrastructure.Auth;
using Planner.Infrastructure.Coordinator;
using Planner.API.Services;
using Planner.Messaging;

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

// API Services
builder.Services.AddScoped<IMatrixCalculationService, MatrixCalculationService>();

// Application / Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
// Auth
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Messaging
builder.Services.AddMessagingBus();
builder.Services.AddScoped<IRouteEnrichmentService, RouteEnrichmentService>();

// Background consumers / coordinators
builder.Services.AddHostedService<CoordinatorService>();
builder.Services.AddHostedService<OptimizeRouteResultConsumer>();

// Health checks
builder.Services.AddHealthChecks();

builder.Services.ConfigureApplicationCookie(options => {
    options.Events.OnRedirectToLogin = context => {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
});

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .AddHttpRequestInterceptor<TenantContextInterceptor>();

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

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

app.MapGraphQL("/graphql");

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
        "JwtOptions:Issuer",
        "JwtOptions:Audience",
        "JwtOptions:SigningKey",
        "JwtOptions:Secret"
    };

    var missing = requiredKeys
        .Where(k => string.IsNullOrWhiteSpace(config[k]))
        .ToList();

    if (missing.Count != 0)
    {
        throw new InvalidOperationException(
            $"Missing required configuration values: {string.Join(", ", missing)}"
        );
    }
}

public partial class Program { }
