using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Planner.Application.OptimizationRuns;

namespace Planner.Application;

public static class ServiceRegistration {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ServiceRegistration).Assembly));
        services.TryAddSingleton<IOptimizationRunNotificationPublisher, NoopOptimizationRunNotificationPublisher>();

        return services;
    }
}
