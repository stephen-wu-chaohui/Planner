# IPlannerDataCenter – Two-Tier Data Access Architecture

## Overview

`IPlannerDataCenter` is the **central data-access facade** for the Planner system. It unifies two memory tiers into a single injection point used by every REST controller and every GraphQL resolver:

| Tier | Name | Technology | Role |
|------|------|-----------|------|
| Long-Term Memory | **The Vault** | SQL Server via EF Core (`IPlannerDbContext`) | Authoritative, durable storage |
| Short-Term Memory | **The Workbench** | HybridCache | Fast cache for frequently-read data |

```
REST Controllers  ──┐
                    ├──► IPlannerDataCenter ──► IPlannerDbContext   (The Vault)
GraphQL Resolvers ──┘                     └──► HybridCache         (The Workbench)
```

---

## Interface

```csharp
public interface IPlannerDataCenter {
    /// The Vault – SQL Server long-term memory
    IPlannerDbContext DbContext { get; }

    /// The Workbench – HybridCache short-term memory
    HybridCache Cache { get; }

    /// Cache-Aside helper (see pattern below)
    Task<T?> GetOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T?>> fetchFromDb,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);
}
```

---

## Cache-Aside Pattern

`GetOrFetchAsync<T>` implements the **Cache-Aside Pattern** in three steps:

```
1. Check The Workbench (HybridCache L1/L2)  → return immediately on HIT
2. Cache MISS → fetch from The Vault (SQL)
3. Store result in The Workbench so subsequent calls are served from cache
```

`HybridCache.GetOrCreateAsync` handles all three steps atomically, including stampede protection (only one factory call runs concurrently for the same key).

### Example

```csharp
// In a controller or GraphQL resolver:
var customers = await dataCenter.GetOrFetchAsync(
    $"customers:{tenantContext.TenantId}",
    () => dataCenter.DbContext.Customers
              .AsNoTracking()
              .Include(c => c.Location)
              .ToListAsync());
```

---

## Dependency Injection Lifetimes

| Service | Lifetime | Reason |
|---------|---------|--------|
| `HybridCache` | Singleton | Registered by `AddHybridCache()` |
| `IPlannerDataCenter` (`PlannerDataCenter`) | Scoped | Must match `IPlannerDbContext` which is scoped per request |

---

## Registration

`AddInfrastructure()` in `ServiceRegistration.cs` handles everything:

```csharp
services.AddInfrastructure(configuration);
```

`AddHybridCache()` registers HybridCache with an in-process (L1) memory tier. No external service is required.

---

## File Map

| Path | Purpose |
|------|---------|
| `src/Planner.Infrastructure/IPlannerDataCenter.cs` | Interface definition |
| `src/Planner.Infrastructure/PlannerDataCenter.cs` | Implementation (Cache-Aside logic) |
| `src/Planner.Infrastructure/ServiceRegistration.cs` | DI registration |
| `test/Planner.Infrastructure.Tests/InfrastructureTests.cs` | Unit tests |

---

## Design Rationale

The two-tier model mirrors established distributed-systems vocabulary:

- **The Vault** (SQL) = durable, transactional, multi-tenant truth. Always consistent; relatively slow.
- **The Workbench** (HybridCache) = fast ephemeral scratch space. Data here is disposable and can always be regenerated from The Vault on a cache miss.

`HybridCache` (introduced in .NET 9) provides:
- **L1**: in-process `IMemoryCache` for zero-serialisation, sub-microsecond reads.
- **L2** (optional): a pluggable `IDistributedCache` backend (e.g. Redis) when cross-process sharing is required. Configure it by registering an `IDistributedCache` alongside `AddHybridCache()`.
- **Stampede protection**: concurrent requests for the same key share a single factory invocation.

Using `IPlannerDataCenter` as the single injection point (rather than injecting `IPlannerDbContext` and `HybridCache` separately) means:

1. Controllers and resolvers stay clean — one dependency, not two.
2. Caching strategy is transparent to call sites that use `GetOrFetchAsync`.
3. It is easy to swap or extend the caching provider without changing any controller.

---

## Rollback Plan

If issues arise with HybridCache in production:

1. **Revert** `ServiceRegistration.cs` to the previous Redis-based registration:
   ```csharp
   services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnectionString);
   ```
2. **Revert** `PlannerDataCenter.cs` to use `IDistributedCache` with the previous Cache-Aside implementation.
3. **Revert** `IPlannerDataCenter.cs` to expose `IDistributedCache Cache` instead of `HybridCache Cache`.
4. **Restore** the Redis service in `docker-compose.yml` and re-add `ConnectionStrings__Redis`.
5. **Restore** `Microsoft.Extensions.Caching.StackExchangeRedis` NuGet reference in the project file.
