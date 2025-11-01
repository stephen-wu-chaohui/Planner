using Planner.Application.Messaging;
using Planner.Infrastructure.SignalR;

namespace Planner.API;

public static class MessageHubStartup
{
    public static IServiceCollection AddMessageHub(this IServiceCollection services, IConfiguration config)
    {
        services.AddSignalR(); // Add SignalR
        services.AddCors(options => {
            var client = config["SignalR:Client"]!;   // your Blazor app
            options.AddPolicy("SignalRPolicy", policy =>
                policy.WithOrigins(client)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

        services.AddSingleton<IMessageHubPublisher, OptimizationResultNotifier>();

        return services;
    }

    public static WebApplication UseMessageHub(this WebApplication app)
    {
        var route = app.Configuration["SignalR:Route"]!;
        app.UseCors("SignalRPolicy");
        app.MapHub<PlannerHub>(route).RequireCors("SignalRPolicy");

        return app;
    }

}

