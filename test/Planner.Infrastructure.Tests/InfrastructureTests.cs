using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Planner.Application;
using Planner.Domain;
using Planner.Infrastructure;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure.Tests;

public class InfrastructureTests
{
    [Fact]
    public async Task DbContext_filters_entities_by_tenant() {
        var tenantId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantContext = Mock.Of<ITenantContext>(
            t => t.TenantId == tenantId
        );

        using var db = new PlannerDbContext(options, tenantContext);

        db.Vehicles.Add(new Vehicle { TenantId = tenantId, Name = "A" });
        db.Vehicles.Add(new Vehicle { TenantId = Guid.NewGuid(), Name = "B" });
        await db.SaveChangesAsync();

        var vehicles = await db.Vehicles.ToListAsync();

        vehicles.Should().HaveCount(1);
    }
}

public class PlannerDataCenterTests
{
    private static (PlannerDbContext db, HybridCache hybridCache) CreateInfrastructure(Guid tenantId) {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantContext = Mock.Of<ITenantContext>(t => t.TenantId == tenantId);
        var db = new PlannerDbContext(options, tenantContext);

        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var hybridCache = sp.GetRequiredService<HybridCache>();

        return (db, hybridCache);
    }

    [Fact]
    public void DataCenter_exposes_DbContext_and_Cache() {
        var tenantId = Guid.NewGuid();
        var (db, hybridCache) = CreateInfrastructure(tenantId);

        var dataCenter = new PlannerDataCenter(db, hybridCache);

        dataCenter.DbContext.Should().BeSameAs(db);
        dataCenter.Cache.Should().BeSameAs(hybridCache);
    }

    [Fact]
    public async Task GetOrFetchAsync_returns_value_from_db_on_cache_miss() {
        var tenantId = Guid.NewGuid();
        var (db, hybridCache) = CreateInfrastructure(tenantId);

        db.Customers.Add(new Customer { Name = "Alice", TenantId = tenantId, LocationId = 1 });
        await db.SaveChangesAsync();

        var dataCenter = new PlannerDataCenter(db, hybridCache);

        var result = await dataCenter.GetOrFetchAsync(
            "customers:alice",
            () => db.Customers.FirstOrDefaultAsync(c => c.Name == "Alice"));

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetOrFetchAsync_caches_value_and_returns_it_on_second_call() {
        var tenantId = Guid.NewGuid();
        var (db, hybridCache) = CreateInfrastructure(tenantId);

        db.Customers.Add(new Customer { Name = "Bob", TenantId = tenantId, LocationId = 1 });
        await db.SaveChangesAsync();

        var dataCenter = new PlannerDataCenter(db, hybridCache);

        const string key = "customers:bob";

        // First call – cache miss, fetches from DB
        var first = await dataCenter.GetOrFetchAsync(
            key,
            () => db.Customers.FirstOrDefaultAsync(c => c.Name == "Bob"));

        // Remove from DB to prove the second call uses the cache
        // Use the EF-tracked entity from the local identity map, not the deserialized cache copy
        db.Customers.Remove(db.Customers.Local.First(c => c.Name == "Bob"));
        await db.SaveChangesAsync();

        // Second call – should hit the cache and still return the value
        var second = await dataCenter.GetOrFetchAsync(
            key,
            () => db.Customers.FirstOrDefaultAsync(c => c.Name == "Bob"));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task GetOrFetchAsync_returns_null_when_not_found_in_db_or_cache() {
        var tenantId = Guid.NewGuid();
        var (db, hybridCache) = CreateInfrastructure(tenantId);
        var dataCenter = new PlannerDataCenter(db, hybridCache);

        var result = await dataCenter.GetOrFetchAsync<Customer>(
            "customers:nobody",
            () => db.Customers.FirstOrDefaultAsync(c => c.Name == "Nobody"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Cache_RemoveAsync_evicts_entry() {
        var tenantId = Guid.NewGuid();
        var (db, hybridCache) = CreateInfrastructure(tenantId);

        db.Customers.Add(new Customer { Name = "Carol", TenantId = tenantId, LocationId = 1 });
        await db.SaveChangesAsync();

        var dataCenter = new PlannerDataCenter(db, hybridCache);
        const string key = "customers:carol";

        // Prime the cache
        var before = await dataCenter.GetOrFetchAsync(
            key,
            () => db.Customers.FirstOrDefaultAsync(c => c.Name == "Carol"));

        // Remove from DB so next DB fetch returns null
        // Use the EF-tracked entity from the local identity map, not the deserialized cache copy
        db.Customers.Remove(db.Customers.Local.First(c => c.Name == "Carol"));
        await db.SaveChangesAsync();

        // Evict from HybridCache
        await hybridCache.RemoveAsync(key);

        // After eviction, cache miss → DB fetch → null (DB is empty)
        var after = await dataCenter.GetOrFetchAsync(
            key,
            () => db.Customers.FirstOrDefaultAsync(c => c.Name == "Carol"));

        before.Should().NotBeNull();
        after.Should().BeNull();
    }
}
