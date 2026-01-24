using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planner.API.Controllers;
using Planner.API.Services;
using Planner.Infrastructure.Persistence;
using Planner.Optimization.DependencyInjection;
using Planner.Application;
using System;
using Planner.Messaging;

namespace Planner.API.EndToEndTests.Fixtures;

public sealed class TestApiFactory : IDisposable {
    public IServiceProvider Services { get; }

    public TestApiFactory() {
        var services = new ServiceCollection();

        // --- EF Core InMemory ---
        services.AddDbContext<PlannerDbContext>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // --- Tenant context ---
        services.AddScoped<ITenantContext, StaticTenantContext>();

        // --- Message Bus ---
        services.AddScoped<IMessageBus, FakeMessageBus>();

        // --- Optimization ---
        services.AddOptimization();

        // --- Matrix Calculation Service ---
        services.AddScoped<IMatrixCalculationService, MatrixCalculationService>();

        // --- Controller ---
        services.AddScoped<OptimizationController>();

        Services = services.BuildServiceProvider();
    }

    public T Get<T>() where T : notnull =>
        Services.GetRequiredService<T>();

    public void Dispose() {
        if (Services is IDisposable d)
            d.Dispose();
    }

    public class StaticTenantContext : ITenantContext {
        public Guid TenantId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public bool IsSet => throw new NotImplementedException();

        public void SetTenant(Guid tenantId) {
            throw new NotImplementedException();
        }
    }
}
