using Planner.Application;
using System.Security;

namespace Planner.API.Middleware;

public sealed class TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext) {
        if (context.User.Identity?.IsAuthenticated == true) {
            var tenantClaim = context.User.FindFirst("tenant_id");

            if (tenantClaim == null) {
                logger.LogWarning("Authenticated user {User} has no tenant_id claim", context.User.Identity.Name ?? "Unknown");
                throw new SecurityException("Authenticated user has no tenant_id claim.");
            }

            if (!Guid.TryParse(tenantClaim.Value, out var tenantId)) {
                logger.LogWarning("Invalid tenant_id claim value: {TenantClaimValue}", tenantClaim.Value);
                throw new SecurityException("Invalid tenant_id claim.");
            }

            tenantContext.SetTenant(tenantId);
            logger.LogDebug("Tenant context set to {TenantId} for user {User}", tenantId, context.User.Identity.Name ?? "Unknown");
        } else {
            logger.LogDebug("Unauthenticated request to {Path}, tenant context not set", context.Request.Path);
        }

        await next(context);
    }
}
