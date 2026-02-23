using HotChocolate.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Planner.Application;
using Planner.Infrastructure.Persistence;
using System.Security;

namespace Planner.API.Middleware;

public sealed class TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IMemoryCache cache, IPlannerDbContext db) {
        if (context.User.Identity?.IsAuthenticated == true) {
            var email = context.User.Identity?.Name;
            if (!string.IsNullOrEmpty(email)) {
                var cacheKey = $"TenantMapping_{email.ToLower()}";
                
                // 1. Try to get the TenantId from the fast memory cache
                if (!cache.TryGetValue(cacheKey, out Guid tenantId)) {
                    // 2. Cache miss! Go to the database (the slow path)
                    var userRecord = await db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == email);

                    if (userRecord != null) {
                        tenantId = userRecord.TenantId;

                        // 3. Save it in the cache for next time (e.g., 10 minutes)
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                        cache.Set(cacheKey, tenantId, cacheOptions);
                    }
                }

                // 4. Set the scoped context for the rest of the request
                if (tenantId != Guid.Empty) {
                    tenantContext.SetTenant(tenantId);
                    logger.LogDebug("Tenant context set to {TenantId} for user {User}", tenantId, context.User.Identity?.Name ?? "Unknown");
                }
            }
        } else {
            logger.LogDebug("Unauthenticated request to {Path}, tenant context not set", context.Request.Path);
        }

        await next(context);
    }
}
