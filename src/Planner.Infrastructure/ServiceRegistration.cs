using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Planner.Application;
using Planner.Application.OptimizationRuns;
using Planner.Application.Persistence;
using Planner.Infrastructure.OptimizationRuns;
using Planner.Infrastructure.Persistence;
using Planner.Infrastructure.ServiceBus;
using Planner.Messaging.Messaging;

namespace Planner.Infrastructure;

public static class ServiceRegistration {
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config) {

        services.AddDbContext<PlannerDbContext>(opt =>
            opt.UseSqlServer(
                config.GetConnectionString("PlannerDb"),
                sql => sql.MigrationsAssembly(
                    typeof(PlannerDbContext).Assembly.FullName)));

        services.AddScoped<IPlannerDbContext>(sp => sp.GetRequiredService<PlannerDbContext>());

        // The Workbench: HybridCache short-term memory (Cache-Aside Pattern).
        // L1 = in-process memory cache; no external cache dependency required.
        services.AddHybridCache();

        services.AddScoped<IPlannerDataCenter, PlannerDataCenter>();
        services.AddOptimizationRunInfrastructure();

        return services;
    }

    public static IServiceCollection AddOptimizationRunInfrastructure(this IServiceCollection services) {
        services.TryAddSingleton<IOptimizationRunStore, CosmosOptimizationRunStore>();
        services.TryAddSingleton<IOptimizationJobQueue, ServiceBusOptimizationJobQueue>();
        return services;
    }

    public static IServiceCollection AddAzureOptimizationMessaging(this IServiceCollection services) {
        services.AddOptimizationRunInfrastructure();
        services.AddHttpClient(AzureOptimizationMessageBus.HttpClientName);
        services.AddSingleton<IMessageBus, AzureOptimizationMessageBus>();
        return services;
    }
}
