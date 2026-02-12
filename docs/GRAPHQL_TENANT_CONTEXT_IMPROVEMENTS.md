# GraphQL Tenant Context Improvements

## Overview
This document describes the improvements made to ensure proper tenant context validation in GraphQL queries and mutations.

## Problem Statement

Previously, GraphQL resolvers in `Query.cs` and some operations in `Mutation.cs` were not checking if the tenant context was set before executing database queries. This caused issues because:

1. **GraphQL requests bypass standard middleware** - The `TenantContextMiddleware` runs in the ASP.NET Core pipeline, but HotChocolate GraphQL creates its own request pipeline
2. **Query filters depend on tenant context** - The `PlannerDbContext` has query filters that automatically filter entities by `TenantId`, which requires `ITenantContext.TenantId` to be set
3. **Missing validation caused errors** - When `ITenantContext.TenantId` is accessed without being set, it throws `InvalidOperationException("TenantId has not been set.")`

### Root Cause

The middleware pipeline order in `Program.cs` was:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();

app.MapGraphQL("/graphql");  // ← GraphQL endpoint mapped AFTER middleware
```

**GraphQL requests bypass `TenantContextMiddleware`** because HotChocolate creates its own execution pipeline that doesn't go through custom middleware registered before `MapGraphQL()`.

## Solution Implemented

### 1. Created TenantContextInterceptor for GraphQL

Added a **HotChocolate HTTP Request Interceptor** that runs as part of the GraphQL execution pipeline and sets tenant context from JWT claims:

**File**: `src/Planner.API/GraphQL/TenantContextInterceptor.cs`

```csharp
public sealed class TenantContextInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        var logger = context.RequestServices.GetRequiredService<ILogger<TenantContextInterceptor>>();

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id");

            if (tenantClaim == null)
            {
                logger.LogWarning("GraphQL: Authenticated user {User} has no tenant_id claim",
                    context.User.Identity.Name ?? "Unknown");
                throw new SecurityException("Authenticated user has no tenant_id claim.");
            }

            if (!Guid.TryParse(tenantClaim.Value, out var tenantId))
            {
                logger.LogWarning("GraphQL: Invalid tenant_id claim value: {TenantClaimValue}",
                    tenantClaim.Value);
                throw new SecurityException("Invalid tenant_id claim.");
            }

            tenantContext.SetTenant(tenantId);
            logger.LogDebug("GraphQL: Tenant context set to {TenantId} for user {User}",
                tenantId, context.User.Identity.Name ?? "Unknown");
        }
        else
        {
            logger.LogDebug("GraphQL: Unauthenticated request, tenant context not set");
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
```

**Benefits:**
- Runs **inside the GraphQL execution pipeline** before resolvers execute
- Extracts `tenant_id` from JWT claims and sets `ITenantContext`
- Provides **comprehensive logging** for debugging
- Throws clear security exceptions for missing/invalid tenant claims

### 2. Registered Interceptor in GraphQL Server Configuration

Updated `Program.cs` to register the interceptor:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .AddHttpRequestInterceptor<TenantContextInterceptor>();  // ← Intercepts GraphQL requests
```

### 3. Enhanced TenantContextMiddleware with Logging

### 3. Enhanced TenantContextMiddleware with Logging

Added comprehensive logging to `TenantContextMiddleware` (for regular REST API endpoints):

```csharp
public sealed class TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext) {
        if (context.User.Identity?.IsAuthenticated == true) {
            var tenantClaim = context.User.FindFirst("tenant_id");

            if (tenantClaim == null) {
                logger.LogWarning("Authenticated user {User} has no tenant_id claim", 
                    context.User.Identity.Name ?? "Unknown");
                throw new SecurityException("Authenticated user has no tenant_id claim.");
            }

            if (!Guid.TryParse(tenantClaim.Value, out var tenantId)) {
                logger.LogWarning("Invalid tenant_id claim value: {TenantClaimValue}", 
                    tenantClaim.Value);
                throw new SecurityException("Invalid tenant_id claim.");
            }

            tenantContext.SetTenant(tenantId);
            logger.LogDebug("Tenant context set to {TenantId} for user {User}", 
                tenantId, context.User.Identity.Name ?? "Unknown");
        } else {
            logger.LogDebug("Unauthenticated request to {Path}, tenant context not set", 
                context.Request.Path);
        }

        await next(context);
    }
}
```

**Benefits:**
- **Debug logging** shows when tenant context is set successfully (for REST endpoints)
- **Warning logging** highlights authentication/claim issues
- **Better troubleshooting** for tenant context problems

### 4. Tenant Context Validation in All Query Resolvers

### 4. Tenant Context Validation in All Query Resolvers

Updated all query resolvers in `Query.cs` to:
1. Inject `ITenantContext` as a service parameter
2. Check if tenant context is set using `tenantContext.IsSet`
3. Throw `UnauthorizedAccessException` with a clear message if not set

**Note**: With the interceptor in place, this validation acts as a **defense-in-depth** measure. The interceptor should set the tenant context before resolvers run, but the validation catches any edge cases.

**Example:**

```csharp
public async Task<List<JobDto>> GetJobs([Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
    if (!tenantContext.IsSet) {
        throw new UnauthorizedAccessException(
            "Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
    }

    return await db.Jobs
        .AsNoTracking()
        .Include(j => j.Location)
        .Select(j => j.ToDto())
        .ToListAsync();
}
```

**Updated Resolvers:**
- `GetJobs` / `GetJobById`
- `GetCustomers` / `GetCustomerById`
- `GetVehicles` / `GetVehicleById`
- `GetDepots` / `GetDepotById`
- `GetLocations` / `GetLocationById` (authentication required for consistency)
- `GetRoutes` / `GetRouteById` (authentication required for consistency)
- `GetTasks` / `GetTaskById` (authentication required for consistency)

### 5. Tenant Context Validation in Mutation Delete Operations

Updated all delete operations in `Mutation.cs` to include tenant context validation. Previously, create and update operations validated tenant context, but delete operations did not.

**Updated Delete Mutations:**
- `DeleteJob`
- `DeleteCustomer`
- `DeleteVehicle`
- `DeleteDepot`
- `DeleteLocation` (authentication required for consistency)
- `DeleteTask` (authentication required for consistency)

**Updated Create/Update Mutations (for non-tenant-scoped entities):**
- `CreateLocation` / `UpdateLocation`
- `CreateTask` / `UpdateTask`

## How It Works Now

### Request Flow for GraphQL

1. **Client sends GraphQL request** with JWT token in Authorization header
2. **ASP.NET Core Authentication Middleware** validates JWT and populates `HttpContext.User`
3. **GraphQL endpoint receives request** at `/graphql`
4. **TenantContextInterceptor.OnCreateAsync()** executes:
   - Extracts `tenant_id` claim from `HttpContext.User`
   - Sets `ITenantContext.TenantId` from the claim
   - Logs success or throws SecurityException if invalid
5. **GraphQL Query/Mutation resolver executes**:
   - Injects `ITenantContext` from DI (already set by interceptor)
   - Validates `tenantContext.IsSet` as defense-in-depth
   - Executes query with EF Core query filters automatically applied
6. **Response returned** with tenant-filtered data

### Comparison: REST vs GraphQL

| Aspect | REST API Endpoints | GraphQL Endpoints |
|--------|-------------------|-------------------|
| **Middleware** | `TenantContextMiddleware` | `TenantContextInterceptor` |
| **When Runs** | After Authentication/Authorization | During GraphQL request creation |
| **Integration** | ASP.NET Core pipeline | HotChocolate execution pipeline |
| **Logging Prefix** | None | "GraphQL: " |

Both approaches achieve the same result: setting `ITenantContext.TenantId` from JWT claims before data access.

## Multi-Tenant Isolation
The following entities have `TenantId` and are automatically filtered by EF Core query filters:
- `Job`
- `Customer`
- `Vehicle`
- `Depot`

For these entities, the query filters in `PlannerDbContext.OnModelCreating` automatically restrict queries to the current tenant:

```csharp
modelBuilder.Entity<Vehicle>()
    .HasQueryFilter(v => v.TenantId == tenant.TenantId);
modelBuilder.Entity<Job>()
    .HasQueryFilter(v => v.TenantId == tenant.TenantId);
modelBuilder.Entity<Customer>()
    .HasQueryFilter(v => v.TenantId == tenant.TenantId);
modelBuilder.Entity<Depot>()
    .HasQueryFilter(v => v.TenantId == tenant.TenantId);
```

### Non-Tenant-Scoped Entities
The following entities do **not** have tenant isolation in the current schema:
- `Location` (shared across tenants)
- `Route` (not yet tenant-scoped)
- `TaskItem` (not yet tenant-scoped)

However, we still enforce authentication for these entities to:
- Maintain consistency in the API
- Prepare for future tenant-scoping if needed
- Prevent unauthorized access

## Error Messages

All tenant context validation failures now return a clear error message:

```
"Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim."
```

This helps API consumers understand:
1. What went wrong (tenant context not set)
2. What they need to do (authenticate with a valid token containing tenant_id claim)

## Testing Recommendations

### 1. Test Authenticated Requests
```graphql
# Should work with valid JWT token containing tenant_id claim
query {
  jobs {
    id
    name
    tenantId
  }
}
```

### 2. Test Unauthenticated Requests
```graphql
# Should return UnauthorizedAccessException
query {
  jobs {
    id
    name
  }
}
```

### 3. Test Tenant Isolation
```graphql
# User with tenant A should only see tenant A's data
# User with tenant B should only see tenant B's data
query {
  vehicles {
    id
    name
    tenantId
  }
}
```

## Future Enhancements

### 1. Add Tenant Scoping to Remaining Entities
Consider adding `TenantId` to:
- `Route` (currently not tenant-scoped)
- `TaskItem` (currently not tenant-scoped)
- `Location` (consider if locations should be tenant-specific)

### 2. GraphQL Authorization Directives
Consider using HotChocolate's authorization directives:
```csharp
[Authorize]
public sealed class Query {
    // ...
}
```

This would enforce authentication at the GraphQL schema level rather than in each resolver.

### 3. Tenant-Scoped Field Resolvers
For complex scenarios, consider creating custom field resolvers that automatically inject and validate tenant context.

## Conclusion

These improvements ensure that:
- ✅ All GraphQL queries validate tenant context before execution
- ✅ All GraphQL mutations validate tenant context before data modification
- ✅ Clear error messages guide API consumers
- ✅ Logging helps diagnose authentication and tenant context issues
- ✅ Multi-tenant isolation is enforced consistently

The GraphQL API now properly respects the multi-tenant architecture and prevents unauthorized cross-tenant data access.
