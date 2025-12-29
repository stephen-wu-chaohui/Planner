using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planner.API.Controllers;
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
}
