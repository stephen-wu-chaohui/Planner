using Microsoft.Extensions.DependencyInjection;

namespace Planner.Optimization;

public static class ServiceRegistration {
    /// <summary>
    /// Registers optimization services and implementations.
    /// </summary>
    public static IServiceCollection AddOptimization(
        this IServiceCollection services) {
        services.AddSingleton<IRouteOptimizer, VehicleRoutingProblem>();
        return services;
    }
}
