using Planner.Application.Messaging;

namespace Planner.API.SignalR;

public static class ServiceRegistration {
    /// <summary>
    /// Registers infrastructure services (SignalR, persistence, external adapters).
    /// </summary>
    public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration config) {
        services.AddSignalR(); // Add SignalR
        
        var client = config["SignalR:Client"];
        if (!string.IsNullOrWhiteSpace(client)) {
            services.AddCors(options => {
                options.AddPolicy("SignalRPolicy", policy =>
                    policy.WithOrigins(client)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
            });
        }

        services.AddSingleton<IMessageHubPublisher, OptimizationResultNotifier>();

        return services;
    }

    public static WebApplication UseRealtime(this WebApplication app) {
        var route = app.Configuration["SignalR:Route"];
        if (string.IsNullOrWhiteSpace(route)) {
            // Skip hub mapping if route not configured
            app.Logger.LogWarning("SignalR:Route not configured, skipping hub mapping");
            return app;
        }

        var client = app.Configuration["SignalR:Client"];
        if (!string.IsNullOrWhiteSpace(client)) {
            app.UseCors("SignalRPolicy");
        }
        
        app.MapHub<PlannerHub>(route);
        app.Logger.LogInformation("SignalR hub mapped to route: {Route}", route);

        return app;
    }
}
