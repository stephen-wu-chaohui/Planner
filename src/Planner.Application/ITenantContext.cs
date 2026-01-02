namespace Planner.Application;

public interface ITenantContext {
    Guid TenantId { get; }
    bool IsSet { get; }

    void SetTenant(Guid tenantId);
}

