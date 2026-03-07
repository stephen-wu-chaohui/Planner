# IPlannerDataCenter – Two-Tier Data Access Architecture

## Overview

`IPlannerDataCenter` is the **central data-access facade** for the Planner system. It unifies two memory tiers into a single injection point used by every REST controller and every GraphQL resolver:

| Tier | Name | Technology | Role |
|------|------|-----------|------|
| Long-Term Memory | **The Vault** | SQL Server via EF Core (`IPlannerDbContext`) | Authoritative, durable storage |
| Short-Term Memory | **The Workbench** | Redis via `IDistributedCache` | Fast cache for frequently-read data |

```
REST Controllers  ──┐
                    ├──► IPlannerDataCenter ──► IPlannerDbContext   (The Vault)
GraphQL Resolvers ──┘                     └──► IDistributedCache   (The Workbench)
```

---

## Interface

```csharp
public interface IPlannerDataCenter {
    /// The Vault – SQL Server long-term memory
    IPlannerDbContext DbContext { get; }

    /// The Workbench – Redis short-term memory
    IDistributedCache Cache { get; }

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
1. Check The Workbench (Redis)    → return immediately on HIT
2. Cache MISS → fetch from The Vault (SQL)
3. Store result in The Workbench so subsequent calls are served from cache
```

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
| `IDistributedCache` | Singleton | Registered by `AddStackExchangeRedisCache` / `AddDistributedMemoryCache` |
| `IPlannerDataCenter` (`PlannerDataCenter`) | Scoped | Must match `IPlannerDbContext` which is scoped per request |

---

## Registration

`AddInfrastructure()` in `ServiceRegistration.cs` handles everything:

```csharp
services.AddInfrastructure(configuration);
```

### Redis (Production / Development with Redis running)

Add to `appsettings.Development.json` or environment variables:

```json
{
  "ConnectionStrings": {
    "PlannerDb": "...",
    "Redis": "localhost:6379"
  }
}
```

### In-Memory Fallback (Testing / CI / no Redis)

When `ConnectionStrings:Redis` is absent or empty, the infrastructure automatically falls back to `AddDistributedMemoryCache()` – no code changes needed.

---

## Starting Redis (Docker)

```bash
docker run -d --name planner-redis -p 6379:6379 redis:7-alpine
```

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
- **The Workbench** (Redis) = fast ephemeral scratch space. Data here is disposable and can always be regenerated from The Vault on a cache miss.

Using `IPlannerDataCenter` as the single injection point (rather than injecting `IPlannerDbContext` and `IDistributedCache` separately) means:

1. Controllers and resolvers stay clean — one dependency, not two.
2. Caching strategy is transparent to call sites that use `GetOrFetchAsync`.
3. It is easy to swap or extend the caching provider without changing any controller.
