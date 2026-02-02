using Planner.Application;

namespace Planner.Infrastructure.Persistence;

public sealed class TenantContext : ITenantContext {
    private Guid? _tenantId;

    public Guid TenantId =>
        _tenantId ?? throw new InvalidOperationException("TenantId has not been set.");

    public bool IsSet => _tenantId.HasValue;

    public void SetTenant(Guid tenantId) {
        if (_tenantId.HasValue)
            throw new InvalidOperationException("TenantId has already been set for this request.");

        _tenantId = tenantId;
    }
}
