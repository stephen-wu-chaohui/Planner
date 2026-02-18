using Microsoft.AspNetCore.Http;
using Planner.Application;

namespace Planner.Infrastructure;

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext {
    public Guid TenantId {
        get; private set;
    }

    public string? UserEmail =>
        httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public bool IsSet => TenantId != Guid.Empty;

    public void SetTenant(Guid tenantId) {
        TenantId = tenantId;
    }
}
