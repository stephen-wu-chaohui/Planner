using Microsoft.Extensions.DependencyInjection;
using Planner.Contracts.Optimization.Abstractions;

namespace Planner.Optimization.DependencyInjection;

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
