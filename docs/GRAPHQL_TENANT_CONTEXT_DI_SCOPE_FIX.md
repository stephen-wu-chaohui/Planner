# GraphQL Tenant Context - DI Scope Issue Fix

## 🎯 Final Root Cause

The real problem was **Dependency Injection Scoping**:

- `TenantContextMiddleware` sets tenant in **HTTP request scope**
- GraphQL resolvers run in a **separate HotChocolate DI scope**
- Result: Different `ITenantContext` instances → tenant not visible to resolvers

**Evidence from logs:**
```
[Interceptor] TenantContext Instance: 5440666, IsSet: True   ← HTTP request scope
[Resolver]    TenantContext Instance: 17497334, IsSet: False ← GraphQL resolver scope
```

## ✅ Solution: Bridge the Scopes

Use HotChocolate's **global state** to pass TenantId from HTTP scope to resolver scope.

### 1. Interceptor: Extract and Store TenantId

```csharp
public sealed class TenantContextInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // Get tenant from HTTP request scope (set by middleware)
        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        
        // Store TenantId in GraphQL global state
        var tenantId = tenantContext.TenantId;
        requestBuilder.SetGlobalState("TenantId", tenantId);
        
        return base.OnCreateAsync(...);
    }
}
```

### 2. Resolvers: Retrieve and Set TenantId

```csharp
public sealed class Query {
    private static void EnsureTenantContext(IResolverContext context, ITenantContext tenantContext)
    {
        if (tenantContext.IsSet)
            return; // Already set in this scope

        // Get TenantId from GraphQL global state (set by interceptor)
        if (!context.ContextData.TryGetValue("TenantId", out var tenantIdObj) 
            || tenantIdObj is not Guid tenantId)
        {
            throw new UnauthorizedAccessException("Tenant context is not available.");
        }

        // Set tenant in resolver scope
        tenantContext.SetTenant(tenantId);
    }

    public async Task<List<JobDto>> GetJobs(
        IResolverContext context,
        [Service] PlannerDbContext db,
        [Service] ITenantContext tenantContext)
    {
        EnsureTenantContext(context, tenantContext); // ← Sets tenant in resolver scope
        
        return await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .Select(j => j.ToDto())
            .ToListAsync();
    }
}
```

## 🔄 Complete Request Flow

```
1. Client → GraphQL Request + JWT
   ↓
2. ASP.NET Authentication → Validates JWT
   ↓
3. TenantContextMiddleware → Runs in HTTP request scope
   Sets: httpTenantContext.SetTenant(tenantId)
   ↓
4. TenantContextInterceptor → Bridges scopes
   Reads: httpTenantContext.TenantId (from HTTP scope)
   Stores: requestBuilder.SetGlobalState("TenantId", tenantId)
   ↓
5. GraphQL Resolver → Runs in GraphQL scope
   Reads: context.ContextData["TenantId"]
   Sets: resolverTenantContext.SetTenant(tenantId)
   ↓
6. EF Core Query Filters → Apply automatically
   Returns: Tenant-filtered data
```

## 📝 Files Changed

1. **src/Planner.API/GraphQL/TenantContextInterceptor.cs**
   - Changed to store TenantId in GraphQL global state
   - Uses `requestBuilder.SetGlobalState("TenantId", tenantId)`

2. **src/Planner.API/GraphQL/Query.cs**
   - Added `EnsureTenantContext()` helper method
   - All resolvers now inject `IResolverContext` 
   - Extract TenantId from `context.ContextData["TenantId"]`
   - Set tenant in resolver scope

3. **src/Planner.API/GraphQL/Mutation.cs**
   - Same pattern as Query.cs

## 🧪 Testing

**Stop debugger, rebuild, restart:**

```bash
dotnet build
# Start API and BlazorApp
```

**Expected logs:**
```
[GraphQL Interceptor] HTTP Scope - TenantContext Instance: XXXX, IsSet: True
[GraphQL Interceptor] Stored TenantId {guid} in GraphQL context
[Query.GetJobs] EnsureTenantContext set TenantId in resolver scope
```

**Expected result:**
✅ GraphQL queries return data  
✅ No "IsSet: False" errors  
✅ Tenant filtering works correctly

## 🎓 Key Learnings

1. **HotChocolate creates separate DI scopes** - Middleware DI ≠ Resolver DI
2. **Use global state to bridge scopes** - `requestBuilder.SetGlobalState()`
3. **Each scope needs its own tenant context** - Set it in each scope separately
4. **Hash codes reveal instance differences** - Debugging technique for DI issues

## ✅ Status

**Ready to test!** Restart both API and BlazorApp to see GraphQL working with proper tenant isolation.
