using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planner.Infrastructure.Persistence;

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

        return services;
    }
}
