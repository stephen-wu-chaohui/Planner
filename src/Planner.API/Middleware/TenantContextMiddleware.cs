using Planner.Application;
using System.Security;

namespace Planner.API.Middleware;

public sealed class TenantContextMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext) {
        if (context.User.Identity?.IsAuthenticated == true) {
            var tenantClaim = context.User.FindFirst("tenant_id");

            if (tenantClaim == null)
                throw new SecurityException("Authenticated user has no tenant_id claim.");

            if (!Guid.TryParse(tenantClaim.Value, out var tenantId))
                throw new SecurityException("Invalid tenant_id claim.");

            tenantContext.SetTenant(tenantId);
        }

        await next(context);
    }
}
