using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planner.Infrastructure.Cache;
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

        // The Workbench: Redis short-term memory (Cache-Aside Pattern).
        // Falls back to an in-process distributed cache when Redis is not configured.
        var redisConnectionString = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString)) {
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnectionString);
        } else {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICache, RedisCache>();
        services.AddScoped<IPlannerDataCenter, PlannerDataCenter>();

        return services;
    }
}
