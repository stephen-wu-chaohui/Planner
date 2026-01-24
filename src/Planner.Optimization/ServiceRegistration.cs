using Microsoft.Extensions.DependencyInjection;

namespace Planner.Optimization;

public static class ServiceRegistration {
    /// <summary>
    /// Registers optimization services and implementations.
    /// </summary>
    public static IServiceCollection AddOptimization(
        this IServiceCollection services) {
        services.AddScoped<IRouteOptimizer, VehicleRoutingProblem>();
        return services;
    }
}
