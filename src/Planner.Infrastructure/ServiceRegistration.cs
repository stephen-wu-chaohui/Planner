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

        return services;
    }
}
