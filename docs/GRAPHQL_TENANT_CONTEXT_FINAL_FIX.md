# GraphQL Tenant Context - Final Fix

## 🎯 Problem Summary

GraphQL requests were failing with:
```
System.InvalidOperationException: TenantId has already been set for this request.
```

## 🔍 Root Cause Analysis

The tenant context was being **set twice** in the same request:

1. **First**: `TenantContextMiddleware` runs for **all** HTTP requests (line in `Program.cs`: `app.UseMiddleware<TenantContextMiddleware>();`)
   - Sets `tenantContext.SetTenant(tenantId)` ✅
   
2. **Second**: `TenantContextInterceptor` runs for GraphQL requests
   - Tries to call `tenantContext.SetTenant(tenantId)` again ❌
   - `TenantContext.SetTenant()` throws if already set (by design for safety)

### Why This Happened

We initially thought GraphQL requests bypassed the middleware, but:
- **Middleware runs BEFORE endpoint routing** (`app.UseMiddleware<TenantContextMiddleware>()` is before `app.MapGraphQL()`)
- **ALL requests** (REST and GraphQL) go through the middleware pipeline
- The interceptor was redundant and caused a conflict

## ✅ Solution

Changed `TenantContextInterceptor` from **setting** tenant context to **validating** it:

### Before (Wrong):
```csharp
// Tried to SET tenant context (but it was already set!)
tenantContext.SetTenant(tenantId); // ← Throws exception!
```

### After (Correct):
```csharp
// VALIDATE tenant context is already set by middleware
if (!tenantContext.IsSet) {
    throw new SecurityException("Tenant context is not set.");
}
```

## 📝 Files Changed

### 1. `src/Planner.API/GraphQL/TenantContextInterceptor.cs`
**Changed from:** Setting tenant context
**Changed to:** Validating tenant context is set

**Key changes:**
- Removed `tenantContext.SetTenant(tenantId)` call
- Added validation: `if (!tenantContext.IsSet) throw ...`
- Added logging to track validation
- Enforces authentication for all GraphQL requests

### 2. `docs/GRAPHQL_TENANT_CONTEXT_FIX_SUMMARY.md`
Updated documentation to reflect the correct solution.

## 🔄 Request Flow (Correct)

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Client → GraphQL Request + JWT Token                    │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. ASP.NET Authentication → Validates JWT                  │
│    Sets HttpContext.User with claims                       │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. TenantContextMiddleware → Runs for ALL requests        │
│    Extracts tenant_id claim                                │
│    Calls tenantContext.SetTenant(tenantId) ✅              │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. GraphQL Pipeline → Routes to /graphql endpoint         │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. TenantContextInterceptor → Validates tenant is set     │
│    if (!tenantContext.IsSet) throw SecurityException;      │
│    Logs validation success ✅                               │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 6. Query.GetJobs() → Resolver executes                    │
│    ITenantContext is injected (already set)                │
│    EF Core query filters apply automatically               │
│    Returns tenant-filtered data ✅                          │
└─────────────────────────────────────────────────────────────┘
```

## 🧪 Testing

### Step 1: Stop debugger and restart both projects

### Step 2: Expected log output (API console):
```
info: Planner.API.Middleware.TenantContextMiddleware
      Tenant context set to {TenantId} for user {User}

info: Planner.API.GraphQL.TenantContextInterceptor
      [GraphQL Interceptor] Validating tenant context - Instance: {Hash}, IsSet: True
      
info: Planner.API.GraphQL.TenantContextInterceptor
      [GraphQL Interceptor] Tenant context validated for user {User}, TenantId: {TenantId}

info: Planner.API.GraphQL.Query
      [Query.GetJobs] ITenantContext instance hash: {Hash}, IsSet: True
      
info: Planner.API.GraphQL.Query
      [Query.GetJobs] Tenant context IS set, TenantId: {TenantId}
```

### Step 3: Expected result
- ✅ BlazorApp successfully loads jobs/vehicles/customers
- ✅ No exceptions in console
- ✅ GraphQL queries return tenant-filtered data

## 🎓 Key Learnings

1. **Middleware runs for ALL endpoints** - Don't assume GraphQL bypasses middleware
2. **Scoped services are shared within a request** - The same `ITenantContext` instance is used by middleware, interceptors, and resolvers
3. **Defense-in-depth is good** - Validate tenant context at multiple layers (middleware sets it, interceptor validates it, resolvers double-check)
4. **Read error messages carefully** - "TenantId has already been set" was the key clue that we were setting it twice

## ✅ Final Status

- **Problem**: Tenant context set twice, causing exception
- **Solution**: Interceptor validates instead of setting
- **Status**: Ready to test
- **Action**: Restart debugger and verify GraphQL queries work
