namespace Planner.Application;

public interface ITenantContext {
    Guid TenantId { get; }
    string UserEmail { get; }
    bool IsSet { get; }

    void SetTenant(Guid tenantId);
}
